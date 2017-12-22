////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  hanUnhook
// Description: 钩子工具类
// Class URI:   
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Nov 13, 2017
//
////////////////////////////////////////////////////////////////////////////////

#ifndef __HAN_UNHOOK_HPP__
#define __HAN_UNHOOK_HPP__

#pragma once

#include <Windows.h>

class hanUnhook
{
	static BOOL func_unhook (LPVOID func_ptr, WORD param_num, DWORD ret_val)
	{
		// mov eax, 12345678h
		// ret 0004h
		BYTE bBuf [] = { '\xB8', '\x00', '\x00', '\x00', '\x00', '\xC2', '\x00', '\x00' };
		*(DWORD*) (&bBuf [1]) = ret_val;
		*(WORD*) (&bBuf [6]) = param_num * 4;
		DWORD dw = 0, dw2 = 0;
		BOOL bRet = ::VirtualProtect (func_ptr, 8, PAGE_EXECUTE_READWRITE, &dw);
		if (!bRet)
			return FALSE;
		::memcpy (func_ptr, bBuf, 8);
		if (func_ptr != ::VirtualProtect)
			::VirtualProtect (func_ptr, 8, dw, &dw2);
		return TRUE;
	}

public:
	static BOOL do_unhook ()
	{
		func_unhook (::SetWindowsHookA, 2, 1);
		func_unhook (::SetWindowsHookW, 2, 1);
		func_unhook (::UnhookWindowsHook, 2, 1);
		func_unhook (::SetWindowsHookExA, 4, 1);
		func_unhook (::SetWindowsHookExW, 4, 1);
		func_unhook (::UnhookWindowsHookEx, 1, 1);
		return TRUE;
	}
};

#endif //__HAN_UNHOOK_HPP__
