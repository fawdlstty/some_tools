#ifndef __BASE64_HPP__
#define __BASE64_HPP__

#include <iostream>
#include <string>

class Base64
{
public:
	// base64编码
	static std::string encode (unsigned char const* data_src, unsigned int enc_len)
	{
		std::string ret;
		int i = 0, j = 0;
		unsigned char array_src [3], array_enc [4];
		while (enc_len--) {
			array_src [i++] = *(data_src++);
			if (i == 3) {
				array_enc [0] = (array_src [0] & 0xfc) >> 2;
				array_enc [1] = ((array_src [0] & 0x03) << 4) + ((array_src [1] & 0xf0) >> 4);
				array_enc [2] = ((array_src [1] & 0x0f) << 2) + ((array_src [2] & 0xc0) >> 6);
				array_enc [3] = array_src [2] & 0x3f;

				for (i = 0; (i <4); i++)
					ret += base64_chars [array_enc [i]];
				i = 0;
			}
		}
		if (i)
		{
			for (j = i; j < 3; j++)
				array_src [j] = '\0';
			array_enc [0] = (array_src [0] & 0xfc) >> 2;
			array_enc [1] = ((array_src [0] & 0x03) << 4) + ((array_src [1] & 0xf0) >> 4);
			array_enc [2] = ((array_src [1] & 0x0f) << 2) + ((array_src [2] & 0xc0) >> 6);
			for (j = 0; (j < i + 1); j++)
				ret += base64_chars [array_enc [j]];
			while ((i++ < 3))
				ret += '=';
		}
		return ret;
	}

	// base64解码
	static std::string decode (std::string const& data_enc)
	{
		int enc_len = data_enc.size (), i = 0, j = 0, k = 0;
		unsigned char array_enc [4], array_src [3];
		std::string ret;
		while (enc_len-- && (data_enc [k] != '=') && is_base64 (data_enc [k])) {
			array_enc [i++] = data_enc [k]; k++;
			if (i == 4) {
				for (i = 0; i <4; i++)
					array_enc [i] = base64_chars.find (array_enc [i]);
				array_src [0] = (array_enc [0] << 2) + ((array_enc [1] & 0x30) >> 4);
				array_src [1] = ((array_enc [1] & 0xf) << 4) + ((array_enc [2] & 0x3c) >> 2);
				array_src [2] = ((array_enc [2] & 0x3) << 6) + array_enc [3];

				for (i = 0; (i < 3); i++)
					ret += array_src [i];
				i = 0;
			}
		}
		if (i) {
			for (j = 0; j < i; j++)
				array_enc [j] = base64_chars.find (array_enc [j]);
			array_src [0] = (array_enc [0] << 2) + ((array_enc [1] & 0x30) >> 4);
			array_src [1] = ((array_enc [1] & 0xf) << 4) + ((array_enc [2] & 0x3c) >> 2);
			for (j = 0; (j < i - 1); j++) ret += array_src [j];
		}
		return ret;
	}

private:
	static bool is_base64 (unsigned char c) {
		return (isalnum (c) || (c == '+') || (c == '/'));
	}

	static const std::string base64_chars;
};

__declspec (selectany) const std::string Base64::base64_chars =
	"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

#endif //__BASE64_HPP__
