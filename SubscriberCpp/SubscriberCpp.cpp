// SubscriberCpp.cpp : Defines the entry point for the application.
//

#include "stdafx.h"
#include "SubscriberCpp.h"
#include "winsock2.h"
#include "include/pubsub/WebResponse.h"
#include "include/pubsub/WebClient.h"
#include "include/pubsub/Subscription.h"

#include "include/rapidjson/prettywriter.h" // for stringify JSON
#include "include/rapidjson/document.h"
#include <cstdio>
#include <string>
#include <vector>

using namespace rapidjson;

#define MAX_LOADSTRING 100

#define ID_EDIT_CONSOLE		100
#define ID_BTN_SUBSCRIBE	101
#define ID_BTN_UNSUBSCRIBE	102
#define ID_TIMER_PULL		103
#define ID_TIMER_RECV		104
#define ID_LBL_LONG_LAT		105
#define ID_LBL_POS			106

// Global Variables:
HINSTANCE			g_hInst;			// current instance
HWND				g_hWndMenuBar;		// menu bar handle

// Our global variables
HWND g_hwnd;
WebClient g_client("127.0.0.1", 8080);
char g_clientId[50];
bool g_subscribed = false;

// Forward declarations of functions included in this code module:
ATOM			MyRegisterClass(HINSTANCE, LPTSTR);
BOOL			InitInstance(HINSTANCE, int);
LRESULT CALLBACK	WndProc(HWND, UINT, WPARAM, LPARAM);
INT_PTR CALLBACK	About(HWND, UINT, WPARAM, LPARAM);

int WINAPI WinMain(HINSTANCE hInstance,
                   HINSTANCE hPrevInstance,
                   LPTSTR    lpCmdLine,
                   int       nCmdShow)
{
	MSG msg;

	// Perform application initialization:
	if (!InitInstance(hInstance, nCmdShow)) 
	{
		return FALSE;
	}

	HACCEL hAccelTable;
	hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_SUBSCRIBERCPP));

	// Main message loop:
	while (GetMessage(&msg, NULL, 0, 0)) 
	{
		if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg)) 
		{
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}

	return (int) msg.wParam;
}

//
//  FUNCTION: MyRegisterClass()
//
//  PURPOSE: Registers the window class.
//
//  COMMENTS:
//
ATOM MyRegisterClass(HINSTANCE hInstance, LPTSTR szWindowClass)
{
	WNDCLASS wc;

	wc.style         = CS_HREDRAW | CS_VREDRAW;
	wc.lpfnWndProc   = WndProc;
	wc.cbClsExtra    = 0;
	wc.cbWndExtra    = 0;
	wc.hInstance     = hInstance;
	wc.hIcon         = LoadIcon(hInstance, MAKEINTRESOURCE(IDI_SUBSCRIBERCPP));
	wc.hCursor       = 0;
	wc.hbrBackground = (HBRUSH) GetStockObject(WHITE_BRUSH);
	wc.lpszMenuName  = 0;
	wc.lpszClassName = szWindowClass;

	return RegisterClass(&wc);
}

//
//   FUNCTION: InitInstance(HINSTANCE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
BOOL InitInstance(HINSTANCE hInstance, int nCmdShow)
{
    TCHAR szTitle[MAX_LOADSTRING];		// title bar text
    TCHAR szWindowClass[MAX_LOADSTRING];	// main window class name

    g_hInst = hInstance; // Store instance handle in our global variable

    // SHInitExtraControls should be called once during your application's initialization to initialize any
    // of the device specific controls such as CAPEDIT and SIPPREF.
    SHInitExtraControls();

    LoadString(hInstance, IDS_APP_TITLE, szTitle, MAX_LOADSTRING); 
    LoadString(hInstance, IDC_SUBSCRIBERCPP, szWindowClass, MAX_LOADSTRING);

    //If it is already running, then focus on the window, and exit
    g_hwnd = FindWindow(szWindowClass, szTitle);	
    if (g_hwnd) 
    {
        // set focus to foremost child window
        // The "| 0x00000001" is used to bring any owned windows to the foreground and
        // activate them.
        SetForegroundWindow((HWND)((ULONG) g_hwnd | 0x00000001));
        return 0;
    } 

    if (!MyRegisterClass(hInstance, szWindowClass))
    {
    	return FALSE;
    }

    g_hwnd = CreateWindow(szWindowClass, szTitle, WS_VISIBLE,
        CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, NULL, NULL, hInstance, NULL);

    if (!g_hwnd)
    {
        return FALSE;
    }

	// create ui controls
	CreateWindowEx(
		0, L"EDIT",   // predefined class 
		NULL,         // no window title 
		WS_CHILD | WS_VISIBLE | WS_VSCROLL | WS_BORDER |
		ES_LEFT | ES_MULTILINE | ES_WANTRETURN | ES_AUTOVSCROLL, 
		0, 0, 0, 0,   // set size in WM_SIZE message 
		g_hwnd,         // parent window 
		(HMENU)ID_EDIT_CONSOLE,   // edit control ID 
		hInstance, 
		NULL);

	CreateWindow( 
		L"BUTTON",  // Predefined class; Unicode assumed 
		L"Subscribe",      // Button text 
		WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_DEFPUSHBUTTON,  // Styles 
		0, 0, 0, 0,   // set size in WM_SIZE message 
		g_hwnd,     // Parent window
		(HMENU)ID_BTN_SUBSCRIBE,       // No menu.
		hInstance, 
		NULL);      // Pointer not needed.

	CreateWindow( 
		L"BUTTON",
		L"Unsubscribe",
		WS_TABSTOP | WS_VISIBLE | WS_CHILD | BS_DEFPUSHBUTTON | WS_DISABLED,
		0, 0, 0, 0,
		g_hwnd,
		(HMENU)ID_BTN_UNSUBSCRIBE,
		hInstance, 
		NULL);

	CreateWindow( 
		L"STATIC",
		L"Position: ",
		SS_LEFT  | WS_VISIBLE | WS_CHILD,
		0, 0, 0, 0,
		g_hwnd,
		(HMENU)ID_LBL_POS,
		hInstance, 
		NULL);

	CreateWindow( 
		L"STATIC",
		L"N/A",
		SS_LEFT  | WS_VISIBLE | WS_CHILD,
		0, 0, 0, 0,
		g_hwnd,
		(HMENU)ID_LBL_LONG_LAT,
		hInstance, 
		NULL);
    // When the main window is created using CW_USEDEFAULT the height of the menubar (if one
    // is created is not taken into account). So we resize the window after creating it
    // if a menubar is present
    if (g_hWndMenuBar)
    {
        RECT rc;
        RECT rcMenuBar;

        GetWindowRect(g_hwnd, &rc);
        GetWindowRect(g_hWndMenuBar, &rcMenuBar);
        rc.bottom -= (rcMenuBar.bottom - rcMenuBar.top);
		
        MoveWindow(g_hwnd, rc.left, rc.top, rc.right-rc.left, rc.bottom-rc.top, FALSE);
    }

    ShowWindow(g_hwnd, nCmdShow);
    UpdateWindow(g_hwnd);

	sprintf(g_clientId, "SubscriberCpp%d", rand() % 100);

    return TRUE;
}

//
//  FUNCTION: WndProc(HWND, UINT, WPARAM, LPARAM)
//
//  PURPOSE:  Processes messages for the main window.
//
//  WM_COMMAND	- process the application menu
//  WM_PAINT	- Paint the main window
//  WM_DESTROY	- post a quit message and return
//
//
LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    int wmId, wmEvent;
    PAINTSTRUCT ps;
    HDC hdc;

    static SHACTIVATEINFO s_sai;
	
    switch (message) 
    {
        case WM_COMMAND:
            wmId    = LOWORD(wParam); 
            wmEvent = HIWORD(wParam); 
            // Parse the menu selections:
            switch (wmId)
            {
				case ID_BTN_SUBSCRIBE:
					OnSubscribeClick();
					break;
				case ID_BTN_UNSUBSCRIBE:
					OnUnsubscribeClick();
					break;
                case IDM_HELP_ABOUT:
                    DialogBox(g_hInst, (LPCTSTR)IDD_ABOUTBOX, hWnd, About);
                    break;
                case IDM_OK:
                    SendMessage (hWnd, WM_CLOSE, 0, 0);				
                    break;
                default:
                    return DefWindowProc(hWnd, message, wParam, lParam);
            }
            break;
        case WM_CREATE:
            SHMENUBARINFO mbi;

            memset(&mbi, 0, sizeof(SHMENUBARINFO));
            mbi.cbSize     = sizeof(SHMENUBARINFO);
            mbi.hwndParent = hWnd;
            mbi.nToolBarId = IDR_MENU;
            mbi.hInstRes   = g_hInst;

            if (!SHCreateMenuBar(&mbi)) 
            {
                g_hWndMenuBar = NULL;
            }
            else
            {
                g_hWndMenuBar = mbi.hwndMB;
            }

            // Initialize the shell activate info structure
            memset(&s_sai, 0, sizeof (s_sai));
            s_sai.cbSize = sizeof (s_sai);
            break;
        case WM_PAINT:
            hdc = BeginPaint(hWnd, &ps);
            
            // TODO: Add any drawing code here...
            
            EndPaint(hWnd, &ps);
            break;
		case WM_TIMER:
			switch(wParam)
			{
				case ID_TIMER_PULL:
					OnPullData();
					break;
				case ID_TIMER_RECV:
					OnHandleResponse();
					break;
			}
			break;
		case WM_SIZE: 
			OnWindowResize(hWnd, message, wParam, lParam);
			break;
        case WM_DESTROY:
            CommandBar_Destroy(g_hWndMenuBar);
			g_client.CloseConnection();
            PostQuitMessage(0);
            break;

        case WM_ACTIVATE:
            // Notify shell of our activate message
            SHHandleWMActivate(hWnd, wParam, lParam, &s_sai, FALSE);
            break;
        case WM_SETTINGCHANGE:
            SHHandleWMSettingChange(hWnd, wParam, lParam, &s_sai);
            break;

        default:
            return DefWindowProc(hWnd, message, wParam, lParam);
    }
    return 0;
}

// Message handler for about box.
INT_PTR CALLBACK About(HWND hDlg, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
        case WM_INITDIALOG:
            {
                // Create a Done button and size it.  
                SHINITDLGINFO shidi;
                shidi.dwMask = SHIDIM_FLAGS;
                shidi.dwFlags = SHIDIF_DONEBUTTON | SHIDIF_SIPDOWN | SHIDIF_SIZEDLGFULLSCREEN | SHIDIF_EMPTYMENU;
                shidi.hDlg = hDlg;
                SHInitDialog(&shidi);
            }
            return (INT_PTR)TRUE;

        case WM_COMMAND:
            if (LOWORD(wParam) == IDOK)
            {
                EndDialog(hDlg, LOWORD(wParam));
                return TRUE;
            }
            break;

        case WM_CLOSE:
            EndDialog(hDlg, message);
            return TRUE;

    }
    return (INT_PTR)FALSE;
}

// our defined funnctions
void OnWindowResize(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
	int left = 10,
		top = 10,
		buttonHeight = 20,
		spacing = 10,
		y = top,
		x = left;

	MoveWindow(GetDlgItem(hWnd, ID_BTN_SUBSCRIBE), x, y, 90, buttonHeight, TRUE);
	x += 100;
	MoveWindow(GetDlgItem(hWnd, ID_BTN_UNSUBSCRIBE), x, y, 90, buttonHeight, TRUE);

	x = left;
	y += 30;
	MoveWindow(GetDlgItem(hWnd, ID_LBL_POS), x, y, 50, buttonHeight, TRUE);
	x += 50;
	MoveWindow(GetDlgItem(hWnd, ID_LBL_LONG_LAT), x, y, 200, buttonHeight, TRUE);

	y += 30;
	MoveWindow(
		GetDlgItem(hWnd, ID_EDIT_CONSOLE), 
		left, 
		y, 
		LOWORD(lParam) - left * 2, 
		HIWORD(lParam) - y - 10, 
		TRUE);
}

void LogW(TCHAR* message)
{
	HWND hwndEdit = GetDlgItem(g_hwnd,ID_EDIT_CONSOLE);
	int len = GetWindowTextLength(hwndEdit);
	TCHAR buf[1024];

	GetWindowText(hwndEdit, buf, len + 1);
	std::wostringstream os;
	os.unsetf(std::ios::skipws);

	if (len < 900)
		os << buf << _T("\r\n");
	os << ">>" << message;

	SetWindowText(hwndEdit, os.str().data());
}

void Log(const char* message)
{
	int len = strlen(message);
	TCHAR* messageW = new TCHAR[len + 1];
	CharToTChar(messageW, message, len + 1);
	LogW(messageW);
	delete[] messageW;
}

void CharToTChar(TCHAR* dest, const char* src, size_t len)
{
	if(sizeof(TCHAR) == 2)
	{
		std::wstringstream ss;
		ss << src;

		_tcscpy_s(dest, len, ss.str().data());
		return;
	}

	strcpy((char*)dest, src);
}

void OnSubscribeClick()
{
	LogW(TEXT("subscribing..."));
	EnableWindow(GetDlgItem(g_hwnd, ID_BTN_UNSUBSCRIBE), true);
	EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE), false);

	Subscription sub(g_clientId, "position");
	StringBuffer sb;
    PrettyWriter<StringBuffer> writer(sb);
	writer.StartObject();
	writer.String("ClientId");
	writer.String(sub.ClientId);
	writer.String("SubscribedEvent");
	writer.String(sub.SubscribedEvent);
	writer.EndObject();

	g_client.WebClientLog = Log;
	if (g_client.Connect() > 0 && g_client.SendPost("/subscribe", sb.GetString()) > 0)
		WaitForResponse(true);
	else 
	{
		EnableWindow(GetDlgItem(g_hwnd, ID_BTN_UNSUBSCRIBE), false);
		EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE), true);
	}
}

void OnUnsubscribeClick()
{
	LogW(TEXT("unsubscribing..."));
	EnableWindow(GetDlgItem(g_hwnd, ID_BTN_UNSUBSCRIBE), false);
	EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE), true);

	Subscription sub(g_clientId, "position");
	StringBuffer sb;
    PrettyWriter<StringBuffer> writer(sb);
	writer.StartObject();
	writer.String("ClientId");
	writer.String(sub.ClientId);
	writer.String("SubscribedEvent");
	writer.String(sub.SubscribedEvent);
	writer.EndObject();

	g_client.WebClientLog = Log;
	if (g_client.Connect() > 0 && g_client.SendPost("/unsubscribe", sb.GetString()) > 0)
		WaitForResponse(true);
	else 
	{
		EnableWindow(GetDlgItem(g_hwnd, ID_BTN_UNSUBSCRIBE), true);
		EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE), false);
	}
}

void WaitForResponse(bool wait)
{
	if (wait)
	{
		SetTimer(g_hwnd,
			ID_TIMER_RECV,
			3000,
			(TIMERPROC) NULL);
	}
	else 
	{
		KillTimer(g_hwnd, ID_TIMER_RECV);
	}
}

void OnHandleResponse()
{
	WebResponse* res = g_client.GetResponse();
	if (res == NULL || res->Body == NULL || strlen(res->Body) == 0)
	{
		if (res != NULL)
			delete res;
		return;
	}

	// we got the response, no more waiting...
	WaitForResponse(false);

	Document document;
	document.Parse(res->Body);
	char log[100];
	std::wostringstream temp;
	const char* lblLong;
	const char* lblLat;
	bool subscribing = !IsWindowEnabled(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE));

	if (document.IsObject() && document.HasMember("success"))
	{
		if (document["success"].GetBool())
		{
			if (subscribing)
			{
				LogW(TEXT("subscribed to PositionEvent successfully!"));
				LogW(TEXT("listening..."));
				SetTimer(g_hwnd,             // handle to main window 
					ID_TIMER_PULL,            // timer identifier 
					5000,                 // 5-second interval 
					(TIMERPROC) NULL);     // no timer callback 
				g_subscribed = true;
			}
			else 
			{
				LogW(TEXT("unsubscribed to PositionEvent successfully!"));
				LogW(TEXT("stopped listening"));
				KillTimer(g_hwnd, ID_TIMER_PULL);
				g_subscribed = false;
			}
		}
	}
	else if (document.IsObject() && document.HasMember("items"))
	{
		Value& items = document["items"];
		for (SizeType i = 0; i < items.Size(); i++) {
			lblLong = items[i]["EventData"]["Long"].GetString();
			lblLat = items[i]["EventData"]["Lat"].GetString();
			sprintf(log, "position (%s,%s)", lblLong, lblLat);

			Log(log);
			temp << TEXT("(") << lblLong << TEXT(", ") << lblLat << TEXT(")");
			SetWindowText(GetDlgItem(g_hwnd, ID_LBL_LONG_LAT), temp.str().data());
		}

		if (items.Size() > 0)
			LogW(TEXT("listening..."));
	}

	delete res;
}

void OnPullData()
{
	if (IsWindowEnabled(GetDlgItem(g_hwnd, ID_BTN_SUBSCRIBE)) || !g_subscribed)
		return;

	Subscription sub(g_clientId, "position");
	StringBuffer sb;
    PrettyWriter<StringBuffer> writer(sb);
	writer.StartObject();
	writer.String("ClientId");
	writer.String(sub.ClientId);
	writer.String("SubscribedEvent");
	writer.String(sub.SubscribedEvent);
	writer.EndObject();

	g_client.WebClientLog = Log;
	if (g_client.Connect() > 0 && g_client.SendPost("/pull", sb.GetString()) > 0)
		WaitForResponse(true);
}
