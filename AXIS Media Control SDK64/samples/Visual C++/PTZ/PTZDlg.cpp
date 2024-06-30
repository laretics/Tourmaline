// PTZDlg.cpp : implementation file
//

#include "stdafx.h"
#include "PTZ.h"
#include "PTZDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

// CPTZDlg dialog
CPTZDlg::CPTZDlg(CWnd* pParent /*=NULL*/)
  : CDialog(CPTZDlg::IDD, pParent)
{
  m_ipText = _T("0.0.0.0");
  m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CPTZDlg::DoDataExchange(CDataExchange* pDX)
{
  CDialog::DoDataExchange(pDX);
  DDX_Text(pDX, IDC_ipTEXT, m_ipText);
  DDX_Control(pDX, IDC_AXISMEDIACONTROL1, m_AMC);
}

BEGIN_MESSAGE_MAP(CPTZDlg, CDialog)
  ON_WM_PAINT()
  ON_WM_QUERYDRAGICON()
  ON_BN_CLICKED(IDC_CONNECT, OnConnect)
  //}}AFX_MSG_MAP
END_MESSAGE_MAP()


// CPTZDlg message handlers

BOOL CPTZDlg::OnInitDialog()
{
  CDialog::OnInitDialog();

  // Set the icon for this dialog.  The framework does this automatically
  //  when the application's main window is not a dialog
  SetIcon(m_hIcon, TRUE);			// Set big icon
  SetIcon(m_hIcon, FALSE);		// Set small icon

  // TODO: Add extra initialization here

  return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CPTZDlg::OnPaint()
{
  if (IsIconic())
  {
    CPaintDC dc(this); // device context for painting

    SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

    // Center icon in client rectangle
    int cxIcon = GetSystemMetrics(SM_CXICON);
    int cyIcon = GetSystemMetrics(SM_CYICON);
    CRect rect;
    GetClientRect(&rect);
    int x = (rect.Width() - cxIcon + 1) / 2;
    int y = (rect.Height() - cyIcon + 1) / 2;

    // Draw the icon
    dc.DrawIcon(x, y, m_hIcon);
  }
  else
  {
    CDialog::OnPaint();
  }
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CPTZDlg::OnQueryDragIcon()
{
  return static_cast<HCURSOR>(m_hIcon);
}

void CPTZDlg::OnConnect()
{
  try
  {
    CString ctrlURL, presetURL, mediaURL;
    this->UpdateData();
    ctrlURL.Format(_T("http://%s/axis-cgi/com/ptz.cgi"), m_ipText);
    mediaURL.Format(_T("http://%s/axis-cgi/mjpg/video.cgi"), m_ipText);
    presetURL.Format(_T("http://%s/axis-cgi/param.cgi?usergroup=anonymous&action=list&group=PTZ.Preset.P0"), m_ipText);
    // Firmware version 4
    //presetURL.Format(_T("http://%s/axis-cgi/view/param.cgi?action=list&group=PTZ.Preset.P0"), m_ipText);

    //Stops possible streams
    m_AMC.Stop();

    // Set the PTZ control properties
    m_AMC.put_PTZControlURL(ctrlURL);
    m_AMC.put_UIMode((CString)"ptz-absolute");

    // Enable PTZ-position presets from AMC context menu
    m_AMC.put_PTZPresetURL(presetURL);

    // Enable joystick support
    m_AMC.put_EnableJoystick(TRUE);

    // Enable area zoom
    m_AMC.put_EnableAreaZoom(TRUE);

    // Enable one-click-zoom
    //m_AMC.put_OneClickZoom(TRUE);

    // Set overlay settings
    m_AMC.put_EnableOverlays(TRUE);
    m_AMC.put_ClientOverlay(AMC_OVERLAY_CROSSHAIR |
                            AMC_OVERLAY_VECTOR |
                            AMC_OVERLAY_ZOOM);

    // Show the status bar and the tool bar in the AXIS Media Control
    m_AMC.put_ShowStatusBar(true);
    m_AMC.put_ShowToolbar(true);
    m_AMC.put_StretchToFit(true);
    m_AMC.put_EnableContextMenu(true);
    m_AMC.put_ToolbarConfiguration((CString)"default,-mute,-volume,+ptz");

    // Set the media URL and the media type
    m_AMC.put_MediaURL(mediaURL);
    m_AMC.Play();
  }
  catch (COleDispatchException *e)
  {      
    MessageBox(e->m_strDescription);
  }
}
