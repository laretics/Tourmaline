// VideoDlg.cpp : implementation file
//

#include "stdafx.h"
#include "Video.h"
#include "VideoDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CVideoDlg dialog

CVideoDlg::CVideoDlg(CWnd* pParent /*=NULL*/)
  : CDialog(CVideoDlg::IDD, pParent)
{
  //{{AFX_DATA_INIT(CVideoDlg)
  m_File = _T("C:\\AMC_Recording.asf");
  m_ipText = _T("0.0.0.0");
  m_type = _T("h264");
  m_user = _T("");
  m_pass = _T("");
  //}}AFX_DATA_INIT
  // Note that LoadIcon does not require a subsequent DestroyIcon in Win32
  m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CVideoDlg::DoDataExchange(CDataExchange* pDX)
{
  CDialog::DoDataExchange(pDX);
  //{{AFX_DATA_MAP(CVideoDlg)
  DDX_Text(pDX, IDC_FILE, m_File);
  DDX_Text(pDX, IDC_ipTEXT, m_ipText);
  DDX_Control(pDX, IDC_AXISMEDIACONTROL1, m_AMC);
  DDX_CBString(pDX, IDC_COMBO_TYPE, m_type);
  DDX_Text(pDX, IDC_EDIT_USER, m_user);
  DDX_Text(pDX, IDC_EDIT_PASS, m_pass);

  //}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CVideoDlg, CDialog)
  //{{AFX_MSG_MAP(CVideoDlg)
  ON_WM_PAINT()
  ON_WM_MOUSEMOVE()
  ON_WM_QUERYDRAGICON()
  ON_BN_CLICKED(IDC_PLAYLIVE, OnPlayLive)
  ON_BN_CLICKED(IDC_STOPPLAY, OnStopPlay)
  ON_BN_CLICKED(IDC_BROWSE, OnBrowse)
  ON_BN_CLICKED(IDC_StartRecord, OnStartRecord)
  ON_BN_CLICKED(IDC_StopRecord, OnStopRecord)
  ON_BN_CLICKED(IDC_PLAYFILE, OnPlayFile)
  //}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CVideoDlg message handlers

BOOL CVideoDlg::OnInitDialog()
{
  CDialog::OnInitDialog();

  // Set the icon for this dialog.  The framework does this automatically
  //  when the application's main window is not a dialog
  SetIcon(m_hIcon, TRUE);			// Set big icon
  SetIcon(m_hIcon, FALSE);		// Set small icon

  try
  {
    // We set AMC properties here for clarity
    m_AMC.put_StretchToFit(TRUE);
    m_AMC.put_MaintainAspectRatio(TRUE);
    m_AMC.put_ShowStatusBar(TRUE);
    m_AMC.put_BackgroundColor(RGB(0,0,0)); // black
    m_AMC.put_VideoRenderer(AMC_VIDEO_RENDERER_EVR);
    m_AMC.put_EnableOverlays(TRUE);

    // Configure context menu
    m_AMC.put_EnableContextMenu(TRUE); 
    //"-pixcount" to remove pixel counter from context menu
    m_AMC.put_ToolbarConfiguration(_T("+play,+fullscreen,-settings"));

    // AMC messaging setting
    m_AMC.put_Popups(
      AMC_POPUPS_LOGIN_DIALOG // Allow login dialog and show
      | AMC_POPUPS_NO_VIDEO     // "No Video" message when stopped
      //| AMC_POPUPS_MESSAGES     // Yellow-balloon notification
      );

    m_AMC.put_UIMode(_T("digital-zoom"));

  }
  catch (COleDispatchException *e)
  {
    MessageBox(e->m_strDescription);
  }	

  return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CVideoDlg::OnPaint() 
{
  if (IsIconic())
  {
    CPaintDC dc(this); // device context for painting

    SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

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

void CVideoDlg::OnMouseMove(UINT nFlags, CPoint point)
{
  if (m_AMC.get_UIMode() == L"digital-zoom")
  {
    if (m_AMC.get_Status() & AMC_STATUS_PLAYING)
    {
      // set focus to AMC in order to zoom using mouse wheel
      m_AMC.SetFocus();
    }
  }
}

// The system calls this to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CVideoDlg::OnQueryDragIcon()
{
  return static_cast<HCURSOR>(m_hIcon);
}

void CVideoDlg::OnPlayLive() 
{
  this->UpdateData();

  CString anURL = m_ipText;

  // Determine protocol to use if not set
  if (m_ipText.Find((CString)"://") == -1)
  {
    if (m_type.CompareNoCase((CString)"mpeg4") == 0 || m_type.CompareNoCase((CString)"h264") == 0 ||
      m_type.CompareNoCase((CString)"h265") == 0)
    {
      anURL = (CString)"axrtsphttp://" + m_ipText;
    }
    else
    {
      anURL = (CString)"http://" + m_ipText;
    }
  }

  // complete URL
  if (!(anURL[anURL.GetLength() - 1] == '/'))
  {
    anURL += (CString)"/";
  }

  if (m_type.CompareNoCase((CString)"mjpeg") == 0)
  {
    anURL = anURL + (CString)"axis-cgi/mjpg/video.cgi";
  }
  else if (m_type.CompareNoCase((CString)"mpeg4") == 0)
  {
    anURL = anURL + (CString)"mpeg4/media.amp";
  }
  else if (m_type.CompareNoCase((CString)"h264") == 0)
  {
    anURL = anURL + (CString)"axis-media/media.amp?videocodec=h264";
  }
  else if (m_type.CompareNoCase((CString)"h265") == 0)
  {
    anURL = anURL + (CString)"axis-media/media.amp?videocodec=h265";
  }

  //Stop possible streams
  m_AMC.Stop();

  // Set username and password
  m_AMC.put_MediaUsername(m_user);
  m_AMC.put_MediaPassword(m_pass);

  // Set the media URL and the media type
  m_AMC.put_MediaURL(anURL);

  // Starts the download of the mpeg4 stream from the Axis camera/video server
  m_AMC.Play(); 

  // check for stream errors in OnError event
}

void CVideoDlg::OnStopPlay() 
{
  m_AMC.Stop();
}

void CVideoDlg::OnBrowse() 
{
  this->UpdateData(true);

  // Present a dialog to select where to save the snapshot.
  CFileDialog myfileDlg(FALSE, NULL, m_File,
    OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
    (CString)"ASF File (*.asf)|*.asf|All Files (*.*)|*.*||");

  if(myfileDlg.DoModal()==IDOK)
  {
    m_File = myfileDlg.GetPathName();
  }

  this->UpdateData(false);
}

void CVideoDlg::OnStartRecord() 
{

  this->UpdateData(true);

  int	aRecMode = AMC_RECORD_FLAG_AUDIO_VIDEO;
  if (m_type.CompareNoCase((CString)"mjpeg") == 0)
  {
    // Audio recording is not supported for Motion JPEG over HTTP
    aRecMode = AMC_RECORD_FLAG_VIDEO;
  }

  try
  {
    // Starts to record to the specified file.
    m_AMC.StartRecordMedia(m_File, aRecMode, (CString)"");
  }
  catch (COleDispatchException *e)
  {
    if (e->m_scError == E_INVALIDARG)
    {
      MessageBox((CString)"Invalid parameters.");
    }
    else if (e->m_scError == E_ACCESSDENIED)
    {
      MessageBox((CString)"Access denied.");
    }
    else
    {
      MessageBox(e->m_strDescription);
    }
  }	

}

void CVideoDlg::OnStopRecord() 
{
  // Start the recording to the specified file.
  m_AMC.StopRecordMedia();
}

void CVideoDlg::OnPlayFile() 
{
  this->UpdateData(true);

  // Present a dialog to select the file to open.
  CFileDialog myfileDlg(TRUE, NULL, m_File,
    OFN_HIDEREADONLY,
    (CString)"ASF File (*.asf;)|*.asf;");

  if(myfileDlg.DoModal()==IDOK)
  {
    //Stop possible streams
    m_AMC.Stop();

    m_AMC.put_MediaFile(myfileDlg.GetPathName());

    // Begin to play the file
    m_AMC.Play();
  }

  this->UpdateData(false);
}

BEGIN_EVENTSINK_MAP(CVideoDlg, CDialog)
  ON_EVENT(CVideoDlg, IDC_AXISMEDIACONTROL1, 8, CVideoDlg::OnStatusChangeAxismediacontrol1, VTS_I4 VTS_I4)
  ON_EVENT(CVideoDlg, IDC_AXISMEDIACONTROL1, 1, CVideoDlg::OnErrorAxismediacontrol1, VTS_I4 VTS_BSTR)
END_EVENTSINK_MAP()


void CVideoDlg::OnStatusChangeAxismediacontrol1(long theNewStatus, long theOldStatus)
{
  // The status of AMC could be monitored here

  if ((theOldStatus & AMC_STATUS_OPENING) > 0 && // was opening
    (theNewStatus & AMC_STATUS_PLAYING) > 0 && // is playing
    (theNewStatus & AMC_STATUS_OPENING) == 0) // is not opening
  {
    //MessageBox(L"Play started successfully...");
  }

  if ((theOldStatus & AMC_STATUS_RECORDING) == 0 && // was not recording
    (theNewStatus & AMC_STATUS_RECORDING) > 0) // is recording
  {
    //MessageBox(L"Recording...");
  }
}


void CVideoDlg::OnErrorAxismediacontrol1(long theErrorCode, LPCTSTR theErrorInfo)
{
  CString aCaption;
  aCaption.Format(_T("Error: 0x%X"), theErrorCode);
  MessageBox((CString)theErrorInfo, aCaption);
}
