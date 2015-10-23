#pragma once
#include "resourceppc.h"

void LogW(TCHAR* message);
void Log(const char* message);
void CharToTChar(TCHAR* dest, const char* src, size_t len);
void OnWindowResize(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam);
void OnSubscribeClick();
void OnUnsubscribeClick();
void OnHandleResponse();
void OnPullData();
void WaitForResponse(bool wait);