using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WallpaperController : MonoBehaviour
{
    #region WinAPI Imports
    // 윈도우 OS의 핵심 기능을 가져옵니다.
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    #endregion

    // 바탕화면 뒤로 보낼 때 필요한 매직 넘버들
    private const int GWL_STYLE = -16;
    private const int WS_BORDER = 0x00800000;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_SYSMENU = 0x00080000;
    private const int WS_MINIMIZEBOX = 0x00020000;

    private const uint SWP_SHOWWINDOW = 0x0040;

    void Start()
    {
        // 빌드된 게임에서만 실행 (에디터에서는 실행 방지)
#if !UNITY_EDITOR
        InitializeWallpaper();
#endif
    }

    void InitializeWallpaper()
    {
        // 1. 현재 실행 중인 유니티 윈도우 핸들 찾기
        // 주의: "ProjectName" 부분을 빌드할 때 설정한 'Product Name'과 똑같이 맞춰야 찾을 수 있습니다.
        // 혹은 null을 넣고 Application.productName을 쓸 수도 있지만, 가장 확실한 건 윈도우 타이틀입니다.
        IntPtr unityWindow = FindWindow(null, Application.productName);

        if (unityWindow == IntPtr.Zero)
        {
            Debug.LogError("유니티 창을 찾을 수 없습니다. Product Name을 확인하세요.");
            return;
        }

        // 2. 윈도우 테두리 제거 (깔끔하게 전체화면처럼 보이게)
        int style = GetWindowLong(unityWindow, GWL_STYLE);
        SetWindowLong(unityWindow, GWL_STYLE, style & ~(WS_BORDER | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX));

        // 3. Progman(프로그램 매니저) 찾기
        IntPtr progman = FindWindow("Progman", null);

        // 4. Progman에게 0x052C 메시지 보내기
        // 이 메시지를 보내면 윈도우가 'WorkerW'라는 레이어를 바탕화면 아이콘 뒤에 생성합니다.
        SendMessage(progman, 0x052C, 0, 0);

        // 5. 우리가 붙어야 할 진짜 배경화면 레이어(WorkerW) 찾기
        IntPtr workerw = IntPtr.Zero;

        // 모든 윈도우를 뒤져서 SHELLDLL_DefView를 가진 WorkerW의 '다음 형제'를 찾습니다.
        EnumWindows((hwnd, lParam) =>
        {
            IntPtr shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDll != IntPtr.Zero)
            {
                // SHELLDLL_DefView를 가진 WorkerW를 찾았다면, 그 다음 형제가 진짜 배경화면 레이어입니다.
                workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            }
            return true;
        }, IntPtr.Zero);

        if (workerw == IntPtr.Zero)
        {
            // 만약 위 방법으로 못 찾았다면(바탕화면 설정에 따라 다름), 그냥 Progman을 쓸 수도 있습니다.
            workerw = progman;
        }

        // 6. 유니티 창을 WorkerW의 자식으로 설정 (입양 보내기)
        SetParent(unityWindow, workerw);

        // 7. 위치와 크기 조정 (왼쪽 위 0,0에 딱 붙이기)
        // 모니터 해상도에 맞춰 꽉 채웁니다.
        int screenWidth = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;
        SetWindowPos(unityWindow, IntPtr.Zero, 0, 0, screenWidth, screenHeight, SWP_SHOWWINDOW);
    }
}