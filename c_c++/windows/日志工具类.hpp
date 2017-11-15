////////////////////////////////////////////////////////////////////////////////
//
// Class Name:  Log
// Description: 日志工具类
// Class URI:   
// Author:      Fawdlstty
// Author URI:  https://www.fawdlstty.com/
// Version:     0.1
// License:     MIT
// Last Update: Sep 19, 2017
//
////////////////////////////////////////////////////////////////////////////////

#ifndef __LOG_HPP__
#define __LOG_HPP__

#pragma once

#include <iostream>
#include <fstream>
#include <string>
#include <cstring>
#include <chrono>
#include <cstdarg>
#include "../c++11时间转字符串带毫秒.hpp"



class Log
{
public:
	Log (std::string path, bool enable) : m_path (path), m_enable (enable)
	{
		//ofs = new std::ofstream (path, std::ios::app);
	}

	// 打印信息
	void show (const char *format, ...)
	{
		if (!m_enable)
			return;
		static char buf [1024 * 64];
		buf [0] = '\0';
		std::ofstream ofs (m_path, std::ios::app);
		va_list ap;
		va_start (ap, format);
		vsnprintf (buf, sizeof(buf), format, ap);
		va_end (ap);
		ofs << buf << std::endl;
		ofs.flush ();
		ofs.close ();
	}

	// 打印信息
	void show_info (char *file, int line, const char *format, ...)
	{
		if (!m_enable)
			return;
		static char buf [1024 * 64], buf2 [1024 * 64];
		buf [0] = buf2 [0] = '\0';
		va_list ap;
		va_start (ap, format);
		vsnprintf (buf, sizeof(buf), format, ap);
		va_end (ap);

		// 处理目录名称
		char *p = strrchr (file, '/');
		if (p) file = p + 1;
		p = strrchr (file, '\\');
		if (p) file = p + 1;
		_snprintf (buf2, sizeof(buf2), "[%s][%s][%d] %s", Times::format_time ().c_str (), file, line, buf);
		show (buf2);
	}

	// 打印数据
	void show_data (char *file, int line, char *var_name, const char *data, int data_len)
	{
		if (!m_enable)
			return;
		show_info (file, line, "variable \"%s\" data: %s", var_name, data);
		return;
	}

	// 打印数据
	void show_binary (char *file, int line, char *var_name, const char *data, int data_len)
	{
		if (!m_enable)
			return;
		auto _c = [] (char ch) -> unsigned char
		{
			return ch;
		};
		auto _ch = [] (char ch) -> unsigned char
		{
			if (ch <= 0x20)
				ch = '.';
			return ch;
		};

		show_info (file, line, "variable \"%s\" data:", var_name);
		show ("================================================================================");
		int addr = 0;
		std::string str;
		str.resize (128);
		// ================================================================================
		// 00000000 | 00 00 00 00 00 00 00 00   00 00 00 00 00 00 00 00 | ........ ........
		// x         x         x         x         x         x         x         x         x
		// 0         10        20        30        40        50        60        70        80
		// 00000000 |                                                   |                  x
		while (data_len >= 16)
		{
			sprintf (&str [0], "%08X | %02X %02X %02X %02X %02X %02X %02X %02X   %02X %02X %02X %02X %02X %02X %02X %02X | %c%c%c%c%c%c%c%c %c%c%c%c%c%c%c%c", addr,
				_c (data [0]), _c (data [1]), _c (data [2]), _c (data [3]), _c (data [4]), _c (data [5]), _c (data [6]), _c (data [7]),
				_c (data [8]), _c (data [9]), _c (data [10]), _c (data [11]), _c (data [12]), _c (data [13]), _c (data [14]), _c (data [15]),
				_ch (data [0]), _ch (data [1]), _ch (data [2]), _ch (data [3]), _ch (data [4]), _ch (data [5]), _ch (data [6]), _ch (data [7]),
				_ch (data [8]), _ch (data [9]), _ch (data [10]), _ch (data [11]), _ch (data [12]), _ch (data [13]), _ch (data [14]), _ch (data [15]));
			show (str.c_str ());
			data_len -= 16;
			addr += 16;
			data += 16;
		}
		if (data_len > 0)
		{
			sprintf (&str [0], "%08X |                                                   |                  ", addr);
			for (int i = 0; i < data_len; ++i)
			{
				int p = 11 + i * 3 + (i >= 8 ? 2 : 0);
				sprintf (&str [p], "%02X", _c (data [i]));
				str [p + 2] = ' ';
				p = 63 + i + (i >= 8 ? 1 : 0);
				str [p] = _ch (data [i]);
			}
			show (str.c_str ());
		}
		show ("================================================================================");
	}

private:
	//std::ofstream *ofs = nullptr;
	std::string m_path;
	bool m_enable;
};

#endif //__LOG_HPP__
