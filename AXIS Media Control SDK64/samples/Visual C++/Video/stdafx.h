// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#ifndef _SECURE_ATL
#define _SECURE_ATL 1
#endif

#ifndef VC_EXTRALEAN
#define VC_EXTRALEAN		// Exclude rarely-used stuff from Windows headers
#endif

// Modify the following defines if you have to target a platform prior to the ones specified below.
// Refer to MSDN for the latest info on corresponding values for different platforms.
#ifndef WINVER				// Allow use of features specific to Windows XP or later.
#define WINVER 0x0501		// Change this to the appropriate value to target other versions of Windows.
#endif

#ifndef _WIN32_WINNT		// Allow use of features specific to Windows XP or later.                   
#define _WIN32_WINNT 0x0501	// Change this to the appropriate value to target other versions of Windows.
#endif						

#ifndef _WIN32_WINDOWS		// Allow use of features specific to Windows 98 or later.
#define _WIN32_WINDOWS 0x0410 // Change this to the appropriate value to target Windows Me or later.
#endif

#ifndef _WIN32_IE			// Allow use of features specific to IE 6.0 or later.
#define _WIN32_IE 0x0600	// Change this to the appropriate value to target other versions of IE.
#endif

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit

// turns off MFC's hiding of some common and often safely ignored warning messages
#define _AFX_ALL_WARNINGS

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions


#include <afxdisp.h>        // MFC Automation classes



#ifndef _AFX_NO_OLE_SUPPORT
#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#endif
#ifndef _AFX_NO_AFXCMN_SUPPORT
#include <afxcmn.h>			// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT
#include <afxdisp.h>

enum AMC_POPUPS
{
  /// AMC will not show any Popups.
  AMC_POPUPS_NONE		      =	0L,

  /// AMC will show a Login Dialog if the username or password is incorrect.
  AMC_POPUPS_LOGIN_DIALOG	=	1L,

  /// All messages are shown as Popups.
  AMC_POPUPS_MESSAGES			=	2L,

  /// "No Video" overlay is shown when the stream is stopped.
  AMC_POPUPS_NO_VIDEO  = 4L,

  /// Untrusted certificates will be rejected without prompting the user.
  AMC_POPUPS_SUPPRESS_UNTRUSTED_CERTIFICATE_ALERT  = 8L,
};

enum AMC_STATUS
{
  /// AMC is uninitialized
  AMC_STATUS_NONE                           = 0L,

  /// AMC is initialized and ready.
  AMC_STATUS_INITIALIZED                    = 1L,

  /// A media stream/file is playing.
  AMC_STATUS_PLAYING                        = 2L,

  /// Playing of a media stream/file is paused.
  AMC_STATUS_PAUSED                         = 4L,

  /// A media stream/file is being recorded.
  AMC_STATUS_RECORDING                      = 8L,

  /// AMC is opening a media stream/file.
  AMC_STATUS_OPENING                       = 16L,

  /// AMC is performing reconnection.
  AMC_STATUS_RECONNECTING                  = 32L,

  /// Issuing a Pan, Tilt, Zoom command to the device.
  AMC_STATUS_ISSUING_PTZ_COMMAND          = 512L,

  /// An extended text/message is displayed in the status bar.
  AMC_STATUS_EXTENDED_TEXT               = 1024L,

  /// UIMode is set to ptz-absolute.
  AMC_STATUS_PTZ_UIMODE_ABS              = 2048L,

  /// UIMode is set to ptz-relative or ptz-relative-no-cross.
  AMC_STATUS_PTZ_UIMODE_REL              = 4096L,

  /// The stream for receiveing audio is being opened.
  AMC_STATUS_OPENING_RECEIVE_AUDIO      = 65536L,

  /// AMC is opening the stream for transmitting audio.
  AMC_STATUS_OPENING_TRANSMIT_AUDIO    = 131072L,

  /// Receiving audio.
  AMC_STATUS_RECEIVE_AUDIO             = 262144L,

  /// Transmitting audio.
  AMC_STATUS_TRANSMIT_AUDIO            = 524288L,

  /// An audio file is being transmitted.
  AMC_STATUS_TRANSMIT_AUDIO_FILE      = 1048576L,

  /// Recording audio.
  AMC_STATUS_RECORDING_AUDIO          = 2097152L
  };

  enum AMC_VIDEO_RENDERER
  {
    /// Video Mixing Renderer 7
    AMC_VIDEO_RENDERER_VMR7          = 0x00000,

    /// Video Mixing Renderer 9
    AMC_VIDEO_RENDERER_VMR9          = 0x01000,

    /// Enhanced Video Renderer
    AMC_VIDEO_RENDERER_EVR           = 0x10000
  };

  /// Option flags for StartRecordMedia
  enum AMC_RECORD_FLAG
  {
    AMC_RECORD_FLAG_NONE              = 0,

    // Record received audio samples
    AMC_RECORD_FLAG_RECEIVED_AUDIO    = 1,

    // Record transmitted audio samples
    AMC_RECORD_FLAG_TRANSMITTED_AUDIO = 2,

    // If set, audio should be saved in its encoded form. Otherwise in wav(pcm)-format.
    AMC_RECORD_FLAG_ENCODED_AUDIO     = 4,

    // Record video only
    AMC_RECORD_FLAG_VIDEO 			      = 8,

    // Record both video and audio
    AMC_RECORD_FLAG_AUDIO_VIDEO       = 16
  };







#ifdef _UNICODE
#if defined _M_IX86
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
#endif


