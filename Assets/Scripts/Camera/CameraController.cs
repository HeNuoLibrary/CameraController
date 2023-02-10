using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    /// <summary>
    /// 视角类型.
    /// </summary>
    private enum ViewType
    {
        _2D = 0,
        _3D
    }

    /// <summary>
    /// 鼠标按键.
    /// </summary>
    private enum MouseButton
    {
        /// <summary>
        /// 鼠标左键.
        /// </summary>
        LEFT = 0,
        /// <summary>
        /// 鼠标右键.
        /// </summary> 
        RIGHT,
        /// <summary>
        /// 鼠标中键.
        /// </summary>
        MIDDLE
    }

    /// <summary>
    /// 指针样式.
    /// </summary>
    private enum CursorType
    {
        /// <summary>
        /// 默认样式.
        /// </summary>
        DEFAULT = 0,
        /// <summary>
        /// 小手.
        /// </summary>
        HAND,
        /// <summary>
        /// 眼睛.
        /// </summary>
        EYE,
        /// <summary>
        /// 放大镜.
        /// </summary>
        MAGNIFIER
    }

    #region 字段
    private const string MOUSESCROLLWHEEL = "Mouse ScrollWheel";        // 鼠标滚轮.
    private const string MOUSEX = "Mouse X";
    private const string MOUSEY = "Mouse Y";

    #region 输入

    private bool leftAltKeyDown;                                        // 左Alt键-按下.
    private bool rightAltKeyDown;                                       // 右Alt键-按下.
    private bool leftAltKey;                                            // 左Alt键-长按.
    private bool rightAltKey;                                           // 右Alt键-长按.
    private bool leftAltKeyUp;                                          // 左Alt键-抬起.
    private bool rightAltKeyUp;                                         // 右Alt键-抬起.
    private bool leftMouseButtonDown;                                   // 鼠标左键-按下.
    private bool rightMouseButtonDown;                                  // 鼠标右键-按下.
    private bool leftMouseButton;                                       // 鼠标左键-长按.
    private bool rightMouseButton;                                      // 鼠标右键-长按.
    private bool rightMouseButtonUp;                                    // 鼠标右键-抬起.
    private bool middleMouseButton;                                     // 鼠标中键-长按.
    private bool middleMouseButtonUp;                                   // 鼠标中键-抬起.

    #endregion

    private Camera m_Camera;                                            // 相机.
    private Texture2D handCursorTexture;                                // 小手.
    private Texture2D eyeCursorTexture;                                 // 眼睛.
    private Texture2D magnifierCursorTexture;                           // 放大镜.

    private Vector2 hotSpot = Vector2.zero;
    private CursorMode cursorMode = CursorMode.Auto;

    private float angle_X;                                              // 水平方向的角度值，绕Transform的y轴旋转（左右）
    private float angle_Y;                                              // 竖直方向的角度值，绕Transform的x轴旋转（上下）
    private float angle_Z;

    // 切换视角
    private Matrix4x4 defaultOrthProjMat;                               // 相机默认的正交投影矩阵.
    private Matrix4x4 defaultPersProjMat;                               // 相机默认的透视投影矩阵.
    private Matrix4x4 currentProjMat;                                   // 相机当前的投影矩阵.
    private bool isChangingViewType = false;                            // 正在切换视角.
    private float viewTypeLerpTime = 0f;                                // 插值时间.
    private ViewType viewType;                                          // 当前视角.

    [Header("Zoom")]
    // 相机拉近拉远 Zoom
    [SerializeField]
    private float distance = 20f;                      // 相机与lookAroudPos点的距离.
    [SerializeField, Range(1f, 20f)]
    private float zoomSpeed = 10f;                                      // 滚轮缩放速度.
    [SerializeField]
    private float maxDistance = 100f;                  // 相机最远距离.
    [SerializeField]
    private float minDistance = 1f;                    // 相机最近距离.

    [Header("360° Rotate")]
    // Alt+鼠标拖动, 360度观察
    [SerializeField, Range(2f, 10f)]
    private float rotateSensitivity = 5f;                               // 旋转灵敏度.
    private Vector3 lookAroundPos = Vector3.zero;

    // Alt+鼠标右键 放大缩小
    [Header("Field Of View")]
    private float minFov = 20f;                                         // 最小视角.
    private float maxFov = 135f;                                        // 最大视角.
    private Vector3 lastMousePosFov = Vector3.zero;
    [SerializeField, Range(0.01f, 0.1f)]
    private float fovSensitivity = 0.05f;                               // 视角灵敏度.

    // 选中物体聚焦
    private GameObject selectedObj = null;                              // 选中的物体.
    private float raycastMaxDistance = 1000f;
    private LayerMask raycastLayerMask;
    private RaycastHit raycastHit;

    [Header("Focus")]
    [SerializeField, Range(0.1f, 5f)]
    private float focusSpeed = 3.0f;                                    // 聚焦的速度.
    [SerializeField]
    private float focusDistance = 3f;                                   // 聚焦时与物体的距离.
    private bool isFocusing = false;                                    // 相机正看向物体中.
    private float focusTime = 0f;                                       // 聚焦时间.
    private Vector3 cameraInitForward;                                  // 相机最初的前方.
    private Vector3 focusTargetForward;                                 // 聚焦目标前方.
    private Vector3 focusInitPos;                                       // 聚焦前相机初始位置.
    private Vector3 focusTargetPos;                                     // 聚焦目标位置.
    #endregion

    #region Unity_Method

    private void Start()
    {
        Init();
    }

    private void LateUpdate()
    {
        // 更新输入.
        UpdateInput();
        // 切换视角.
        SwitchViewType();
        // 拉近拉远.
        Zoom();
        // 拖动相机.
        Drag();
        // 360°查看.
        LookAround();
        // 放大缩小. 和切换视角共同使用时有Bug.
        Magnifier();
        // 聚焦.
        Focus();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(Vector2.zero, new Vector2(100, 50)), "正交"))
        {
            SetViewType(ViewType._2D);
        }

        if (GUI.Button(new Rect(Vector2.up * 60, new Vector2(100, 50)), "透视"))
        {
            SetViewType(ViewType._3D);
        }

        if (GUI.Button(new Rect(Vector2.up * 120, new Vector2(100, 50)), "正视图"))
        {
            Front();
        }

        if (GUI.Button(new Rect(Vector2.up * 180, new Vector2(100, 50)), "后视图"))
        {
            Back();
        }

        if (GUI.Button(new Rect(Vector2.up * 240, new Vector2(100, 50)), "左视图"))
        {
            Left();
        }

        if (GUI.Button(new Rect(Vector2.up * 300, new Vector2(100, 50)), "右视图"))
        {
            Right();
        }

        if (GUI.Button(new Rect(Vector2.up * 360, new Vector2(100, 50)), "上视图"))
        {
            Top();
        }

        if (GUI.Button(new Rect(Vector2.up * 420, new Vector2(100, 50)), "下视图"))
        {
            Down();
        }
    }

    #endregion

    #region 初始化相关

    /// <summary>
    /// 初始化.
    /// </summary>
    private void Init()
    {
        m_Camera = GetComponent<Camera>();
        raycastLayerMask = 1 << LayerMask.NameToLayer("Default");

        RecalcDistance();
        CalculateProjMatrix();                  // 获取相机默认矩阵.
        ResetLookAroundPos();                   // 重置观察中心.
        LoadCursorTexture();                    // 加载鼠标图标.
    }

    /// <summary>
    /// 重置观察中心(观察中心在相机的前方).
    /// </summary>
    private void ResetLookAroundPos()
    {
        lookAroundPos = m_Camera.transform.position + m_Camera.transform.rotation * (Vector3.forward * distance);
    }

    /// <summary>
    /// 加载鼠标指针图标.
    /// </summary>
    private void LoadCursorTexture()
    {
        handCursorTexture = Resources.Load<Texture2D>("Textures/Hand");
        eyeCursorTexture = Resources.Load<Texture2D>("Textures/Eye");
        magnifierCursorTexture = Resources.Load<Texture2D>("Textures/Magnifier");
    }

    /// <summary>
    /// 设置鼠标样式.
    /// </summary>
    private void SetCursor(CursorType cursorType)
    {
        switch (cursorType)
        {
            case CursorType.DEFAULT:
                Cursor.SetCursor(null, hotSpot, cursorMode);
                break;
            case CursorType.HAND:
                Cursor.SetCursor(handCursorTexture, hotSpot, cursorMode);
                break;
            case CursorType.EYE:
                Cursor.SetCursor(eyeCursorTexture, hotSpot, cursorMode);
                break;
            case CursorType.MAGNIFIER:
                Cursor.SetCursor(magnifierCursorTexture, hotSpot, cursorMode);
                break;
            default:
                Debug.LogError("未知指针类型.");
                break;
        }
    }

    /// <summary>
    /// 更新输入.
    /// </summary>
    private void UpdateInput()
    {
        leftAltKeyDown = Input.GetKeyDown(KeyCode.LeftAlt);                            // 左Alt键-按下.
        rightAltKeyDown = Input.GetKeyDown(KeyCode.RightAlt);                          // 右Alt键-按下.
        leftAltKey = Input.GetKey(KeyCode.LeftAlt);                                    // 左Alt键-长按.
        rightAltKey = Input.GetKey(KeyCode.RightAlt);                                  // 右Alt键-长按.
        leftAltKeyUp = Input.GetKeyUp(KeyCode.LeftAlt);                                // 左Alt键-抬起.
        rightAltKeyUp = Input.GetKeyUp(KeyCode.RightAlt);                              // 右Alt键-抬起.
        rightMouseButtonDown = Input.GetMouseButtonDown((int)MouseButton.RIGHT);       // 鼠标右键-按下.
        leftMouseButtonDown = Input.GetMouseButtonDown((int)MouseButton.LEFT);         // 鼠标左键-按下.
        leftMouseButton = Input.GetMouseButton((int)MouseButton.LEFT);                 // 鼠标左键-长按.
        rightMouseButton = Input.GetMouseButton((int)MouseButton.RIGHT);               // 鼠标右键-长按.
        rightMouseButtonUp = Input.GetMouseButtonUp((int)MouseButton.RIGHT);           // 鼠标右键-抬起.

        middleMouseButton = Input.GetMouseButton((int)MouseButton.MIDDLE);             // 鼠标中键-按下.  
        middleMouseButtonUp = Input.GetMouseButtonUp((int)MouseButton.MIDDLE);         // 鼠标中键-抬起.
    }

    /// <summary>
    /// 计算距离.
    /// </summary>
    private void RecalcDistance()
    {
        Vector3 checkFarPos = m_Camera.transform.position + m_Camera.transform.rotation * (Vector3.forward * 1000);
        if (Physics.Linecast(m_Camera.transform.position, checkFarPos, out raycastHit))
        {
            // 如果相机中心有物体，则以物体到相机为距离
            distance = raycastHit.distance;
            if (selectedObj == null)
            {
                selectedObj = raycastHit.collider.gameObject;
            }
        }
    }

    #endregion

    #region 切换视角

    /// <summary>
    /// 计算投影矩阵.
    /// </summary>
    private void CalculateProjMatrix()
    {
        currentProjMat = m_Camera.projectionMatrix;

        float aspect = m_Camera.aspect;
        float fov = m_Camera.fieldOfView;
        float size = m_Camera.orthographicSize;
        float far = m_Camera.farClipPlane;
        float near = m_Camera.nearClipPlane;
        float tan = Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);

        if (m_Camera.orthographic)
        {
            // 启动时相机是正交的
            viewType = ViewType._2D;
            fov = SetFovBySize(size);
        }
        else
        {
            // 启动时相机是透视的
            viewType = ViewType._3D;
            size = SetSizeByFov(fov);
        }

        // 透视投影矩阵.
        defaultPersProjMat = new Matrix4x4();
        //defaultPersProjMat = Matrix4x4.Perspective(fov, aspect, near, far);           // 透视矩阵也可直接调用这个来计算.
        defaultPersProjMat.SetRow(0, new Vector4(1.0f / (tan * aspect), 0, 0, 0));
        defaultPersProjMat.SetRow(1, new Vector4(0, 1.0f / tan, 0, 0));
        defaultPersProjMat.SetRow(2, new Vector4(0, 0, -(far + near) / (far - near), -2 * far * near / (far - near)));
        defaultPersProjMat.SetRow(3, new Vector4(0, 0, -1, 0));

        // 正交投影矩阵.
        defaultOrthProjMat = new Matrix4x4();
        defaultOrthProjMat.SetRow(0, new Vector4(1.0f / (aspect * size), 0, 0, 0));
        defaultOrthProjMat.SetRow(1, new Vector4(0, 1.0f / size, 0, 0));
        defaultOrthProjMat.SetRow(2, new Vector4(0, 0, -2f / (far - near), -(far + near) / (far - near)));
        defaultOrthProjMat.SetRow(3, new Vector4(0, 0, 0, 1));
    }

    /// <summary>
    /// 切换视角.
    /// </summary>
    private void SwitchViewType()
    {
        if (isChangingViewType)
        {
            viewTypeLerpTime += Time.deltaTime * 2.0f;
            if (viewType == ViewType._2D)
            {
                // 切换到正交视图.
                currentProjMat = Utils.Math.MatrixLerp(currentProjMat, defaultOrthProjMat, viewTypeLerpTime);
            }
            else
            {
                // 切换到透视视图.
                currentProjMat = Utils.Math.MatrixLerp(currentProjMat, defaultPersProjMat, viewTypeLerpTime);
            }
            m_Camera.projectionMatrix = currentProjMat;
            if (viewTypeLerpTime >= 1.0f)
            {
                isChangingViewType = false;
                viewTypeLerpTime = 0f;

                if (viewType == ViewType._2D)
                {
                    m_Camera.orthographic = true;
                }
                else
                {
                    m_Camera.orthographic = false;
                }
                m_Camera.ResetProjectionMatrix();
            }
        }
    }

    /// <summary>
    /// 设置将要切换的视角.
    /// </summary>
    /// <param name="targetViewType">目标视角.</param>
    private void SetViewType(ViewType targetViewType)
    {
        if (viewType != targetViewType)
        {
            viewType = targetViewType;
            isChangingViewType = true;
            viewTypeLerpTime = 0f;
        }
    }

    /// <summary>
    /// 根据正交相机的size设置对应透视相机的fov
    /// </summary>
    /// <param name="size">正交相机高度的一半，即orthographicSize</param>
    private float SetFovBySize(float size)
    {
        float fov = 2.0f * Mathf.Atan(size / distance) * Mathf.Rad2Deg;
        m_Camera.fieldOfView = fov;
        return fov;
    }

    /// <summary>
    /// 根据透视相机的fov设置对应正交相机的即orthographicSize
    /// </summary>
    /// <param name="fov">透视相机的fov</param>
    private float SetSizeByFov(float fov)
    {
        float size = distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        m_Camera.orthographicSize = size;
        return size;
    }

    #endregion

    #region 拉近拉远

    /// <summary>
    /// 拉近拉远.
    /// </summary>
    /// <remarks>鼠标滚轮滚动，向上滚动拉近相机，向下滚动拉远相机</remarks>
    private void Zoom()
    {
        float scrollWheelValue = Input.GetAxis(MOUSESCROLLWHEEL);
        if (!Utils.Math.IsEqual(scrollWheelValue, 0f))
        {
            // scrollWheelValue：滚轮向上为+ 向下为-
            // 同时滚轮向上，拉近相机；滚轮向下，拉远相机
            distance -= scrollWheelValue * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            m_Camera.transform.position = lookAroundPos - m_Camera.transform.rotation * (Vector3.forward * distance);

            // 相机距离改变后需同时修改fov或orthographicSize以及投影矩阵，保证正交/透视视图切换效果
            CalculateProjMatrix();
        }
    }

    #endregion

    #region 拖动

    private bool isDraging = false;
    private Vector3 lastMousePos;

    /// <summary>
    /// 拖动.
    /// </summary>
    /// <remark>长按鼠标中键拖动</remark>
    private void Drag()
    {
        // 鼠标中键拖动相机.
        if (middleMouseButton)
        {
            if (isDraging == false)
            {
                isDraging = true;
                // 设置小手指针.
                SetCursor(CursorType.HAND);

                lastMousePos = Input.mousePosition;
            }
            else
            {
                Vector3 newMousePos = Input.mousePosition;
                Vector3 delta = newMousePos - lastMousePos;
                m_Camera.transform.position += CalcDragLength(m_Camera, delta, distance);
                lastMousePos = newMousePos;

                // 相机移动，重新设置目标位置.
                ResetLookAroundPos();
            }
        }
        if (middleMouseButtonUp)
        {
            isDraging = false;
            // 恢复默认指针.
            SetCursor(CursorType.DEFAULT);
        }
    }

    /// <summary>
    /// 计算拖拽距离.
    /// </summary>
    /// <param name="camera">相机</param>
    /// <param name="mouseDelta">鼠标移动距离</param>
    /// <param name="distance">相机距离观察物体的距离</param>
    private Vector3 CalcDragLength(Camera camera, Vector2 mouseDelta, float distance)
    {
        float rectHeight = -1;
        float rectWidth = -1;
        if (camera.orthographic)
        {
            rectHeight = 2 * camera.orthographicSize;
            //rectWidth = rectHeight / camera.aspect;
        }
        else
        {
            rectHeight = 2 * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
        rectWidth = Screen.width * rectHeight / Screen.height;
        Vector3 moveDir = -rectWidth / Screen.width * mouseDelta.x * camera.transform.right - rectHeight / Screen.height * mouseDelta.y * camera.transform.up;

        return moveDir;
    }

    #endregion

    #region 360°旋转观察

    /// <summary>
    /// 360°旋转查看.
    /// </summary>
    /// <remark>Alt+鼠标左键 或 鼠标右键</remark>
    private void LookAround()
    {
        if (leftAltKeyDown || rightAltKeyDown || rightMouseButtonDown)
        {
            SetCursor(CursorType.EYE);

            angle_X = m_Camera.transform.eulerAngles.y;
            angle_Y = m_Camera.transform.eulerAngles.x;
            angle_Z = m_Camera.transform.eulerAngles.z;
        }

        if ((leftAltKey && leftMouseButton)
            || (rightAltKey && leftMouseButton)
            || (!leftAltKey && !rightAltKey && rightMouseButton))
        {
            float deltaX = Input.GetAxis(MOUSEX);
            float deltaY = Input.GetAxis(MOUSEY);

            // 相机朝前和朝后,与鼠标的滑动方向相反
            if ((angle_Y > 90f && angle_Y < 270f) || (angle_Y < -90 && angle_Y > -270f))
            {
                angle_X -= deltaX * rotateSensitivity;
            }
            else
            {
                angle_X += deltaX * rotateSensitivity;
            }
            angle_Y -= deltaY * rotateSensitivity;

            angle_X = Utils.Math.ClampAngle(angle_X, -365, 365);
            angle_Y = Utils.Math.ClampAngle(angle_Y, -365, 365);

            SetCameraPos();
        }

        if (leftAltKeyUp || rightAltKeyUp || rightMouseButtonUp)
        {
            SetCursor(CursorType.DEFAULT);
        }
    }

    private void SetCameraPos()
    {
        Quaternion rotation = Quaternion.Euler(angle_Y, angle_X, angle_Z);
        Vector3 dir = Vector3.forward * -distance;
        m_Camera.transform.rotation = rotation;
        m_Camera.transform.position = lookAroundPos + rotation * dir;
    }

    #endregion

    #region 聚焦选中物体

    /// <summary>
    /// 聚焦选中物体.
    /// </summary>
    private void Focus()
    {
        if (leftMouseButtonDown)
        {
            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);

            // Scene视图绘制射线方便测试.
            //Debug.DrawLine(ray.origin, ray.origin + ray.direction * raycastMaxDistance, Color.red, 3f);

            if (Physics.Raycast(ray, out raycastHit, raycastMaxDistance, raycastLayerMask))
            {
                selectedObj = raycastHit.collider.gameObject;
            }
            else
            {
                selectedObj = null;
                ResetLookAroundPos();
            }
        }

        if (selectedObj != null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isFocusing = true;
                focusTime = 0f;
                cameraInitForward = m_Camera.transform.forward;
                focusTargetForward = Vector3.Normalize(selectedObj.transform.position - m_Camera.transform.position);

                focusInitPos = m_Camera.transform.position;
                distance = focusDistance;
                focusTargetPos = selectedObj.transform.position - focusTargetForward * distance;
            }
        }

        if (isFocusing)
        {
            focusTime += Time.deltaTime * focusSpeed;
            Vector3 forward = Vector3.Lerp(cameraInitForward, focusTargetForward, focusTime);
            m_Camera.transform.rotation = Quaternion.LookRotation(forward);

            m_Camera.transform.position = Vector3.Lerp(focusInitPos, focusTargetPos, focusTime);

            if (focusTime >= 1f)
            {
                // 聚焦完毕.
                focusTime = 0f;
                isFocusing = false;
                m_Camera.transform.rotation = Quaternion.LookRotation(focusTargetForward);
                m_Camera.transform.position = focusTargetPos;

                // 设置观察中心.
                lookAroundPos = selectedObj.transform.position;
            }
        }
    }

    #endregion

    #region 各种视图

    /// <summary>
    /// 正视图.
    /// </summary>
    private void Front()
    {
        angle_X = 180;
        angle_Y = 0;

        SetCameraPos();
    }

    /// <summary>
    /// 后视图.
    /// </summary>
    private void Back()
    {
        angle_X = 0;
        angle_Y = 0;

        SetCameraPos();
    }

    /// <summary>
    /// 右视图.
    /// </summary>
    private void Right()
    {
        angle_X = -90;
        angle_Y = 0;

        SetCameraPos();
    }

    /// <summary>
    /// 左视图.
    /// </summary>
    private void Left()
    {
        angle_X = 90;
        angle_Y = 0;

        SetCameraPos();
    }

    /// <summary>
    /// 上视图(俯视图).
    /// </summary>
    private void Top()
    {
        angle_X = 0;
        angle_Y = 90;

        SetCameraPos();
    }

    /// <summary>
    /// 下视图.
    /// </summary>
    private void Down()
    {
        angle_X = 0;
        angle_Y = -90;

        SetCameraPos();
    }

    #endregion

    #region 放大/缩小

    /// <summary>
    /// 放大缩小.
    /// </summary>
    private void Magnifier()
    {
        if ((leftAltKey || rightAltKey) && rightMouseButton)
        {
            if (lastMousePosFov.Equals(Vector3.zero))
            {
                lastMousePosFov = Input.mousePosition;
                SetCursor(CursorType.MAGNIFIER);
            }

            float deltaX = lastMousePosFov.x - Input.mousePosition.x;

            if (viewType == ViewType._2D)
            {
                // 透视视图改size
                float size = m_Camera.orthographicSize + deltaX * 0.01f;
                size = Mathf.Clamp(size, 1.0f, 8f);
                m_Camera.orthographicSize = size;

                // 调节size需同时修改相机透视模式下的fov
                SetFovBySize(size);
            }
            else
            {
                // 透视视图改fov
                float fov = m_Camera.fieldOfView + deltaX * fovSensitivity;
                fov = Mathf.Clamp(fov, minFov, maxFov);
                m_Camera.fieldOfView = fov;

                // 调节了fov需同时修改相机正交模式下的size
                SetSizeByFov(fov);
            }

            lastMousePosFov = Input.mousePosition;
        }

        if (rightMouseButtonUp)
        {
            SetCursor(CursorType.DEFAULT);
            lastMousePosFov = Vector3.zero;

            // 重新计算投影矩阵.
            CalculateProjMatrix();
        }
    }

    #endregion
}
