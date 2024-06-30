// AudioSampleDlg.cpp : implementation file
//

#include "stdafx.h"
#include "AudioSample.h"
#include "AudioSampleDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

/////////////////////////////////////////////////////////////////////////////
// CAudioSampleDlg dialog

CAudioSampleDlg::CAudioSampleDlg(CWnd* pParent /*=NULL*/)
  : CDialog(CAudioSampleDlg::IDD, pParent)
{
  //{{AFX_DATA_INIT(CAudioSampleDlg)
  m_ipText = _T("0.0.0.0");
  //}}AFX_DATA_INIT
  // Note that LoadIcon does not require a subsequent DestroyIcon in Win32
  m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CAudioSampleDlg::DoDataExchange(CDataExchange* pDX)
{
  CDialog::DoDataExchange(pDX);
  DDX_Control(pDX, IDC_AXISMEDIACONTROL1, m_AMC);
  DDX_Control(pDX, IDC_BtnPlay, m_BtnPlay);
  DDX_Control(pDX, IDC_StartTransmitMedia, m_BtnStartTransmitMedia);
  DDX_Control(pDX, IDC_GroupBoxReceive, m_GroupBoxReceive);
  DDX_Control(pDX, IDC_StartRecordMedia, m_StartRecordMedia);
  DDX_Control(pDX, IDC_CheckBoxTransmitted, m_CheckBoxTransmitted);
  DDX_Control(pDX, IDC_CheckBoxReceived, m_CheckBoxReceived);
  DDX_Text(pDX, IDC_ipText, m_ipText);
}

BEGIN_MESSAGE_MAP(CAudioSampleDlg, CDialog)
  ON_WM_PAINT()
  ON_WM_QUERYDRAGICON()
  ON_BN_CLICKED(IDC_StartRecordMedia, OnStartRecordMedia)
  ON_BN_CLICKED(IDC_StartTransmitMedia, OnStartTransmitMedia)
  ON_BN_CLICKED(IDC_RdbReceiveOn, OnRdbReceiveOn)
  ON_BN_CLICKED(IDC_RdbReceiveOff, OnRdbReceiveOff)
  ON_BN_CLICKED(IDC_RdbTransmitOn, OnRdbTransmitOn)
  ON_BN_CLICKED(IDC_RdbTransmitOff, OnRdbTransmitOff)
  ON_BN_CLICKED(IDC_CheckBoxReceived, OnCheckBoxReceived)
  ON_BN_CLICKED(IDC_CheckBoxTransmitted, OnCheckBoxTransmitted)
  ON_BN_CLICKED(IDC_Connect, OnConnect)
  ON_BN_CLICKED(IDC_BtnPlay, OnBtnPlay)
  //}}AFX_MSG_MAP
END_MESSAGE_MAP()


// CAudioSampleDlg message handlers

BOOL CAudioSampleDlg::OnInitDialog()
{
  CDialog::OnInitDialog();

  // Set the icon for this dialog.  The framework does this automatically
  //  when the application's main window is not a dialog
  SetIcon(m_hIcon, TRUE);			// Set big icon
  SetIcon(m_hIcon, FALSE);		// Set small icon
  
  CButton* myRadioButton;
  myRadioButton = (CButton*)GetDlgItem(IDC_RdbReceiveOff);
  myRadioButton->SetCheck(1);
  myRadioButton = (CButton*)GetDlgItem(IDC_RdbTransmitOff);
  myRadioButton->SetCheck(1);

  return TRUE;  // return TRUE  unless you set the focus to a control
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CAudioSampleDlg::OnPaint()
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

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CAudioSampleDlg::OnQueryDragIcon()
{
  return static_cast<HCURSOR>(m_hIcon);
}

void CAudioSampleDlg::OnStartRecordMedia() 
{
  CString aBtnCaption;
  m_StartRecordMedia.GetWindowText(aBtnCaption);

  if (aBtnCaption.Find((CString)"Start") != -1)
  {
    int theFlags = AMC_RECORD_FLAG_NONE;

    if(m_CheckBoxReceived.GetCheck())
    {
      // Recording is only supported in 8 kHz
      MessageBox(_T("Make sure the Axis device is configured to send audio in 8 kHz."));
      theFlags |= AMC_RECORD_FLAG_RECEIVED_AUDIO;
    }

    if(m_CheckBoxTransmitted.GetCheck())
    {
      theFlags |= AMC_RECORD_FLAG_TRANSMITTED_AUDIO;
    }

    // Present a dialog to select a file
    CFileDialog myfileDlg(FALSE, NULL, (CString)"*.wav",
      OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
      (CString)"Audio Clip (*.wav)| All Files (*.*)|*.*||");

    if(myfileDlg.DoModal ()==IDOK )
    {
      m_StartRecordMedia.SetWindowText((CString)"Stop");
      this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(false);
      this->GetDlgItem(IDC_CheckBoxReceived)->EnableWindow(false);
      this->GetDlgItem(IDC_CheckBoxTransmitted)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxRecord)->EnableWindow(false);
      this->GetDlgItem(IDC_StartTransmitMedia)->EnableWindow(false);

      try
      {
        // // Stores one or more media streams to a file.
        m_AMC.StartRecordMedia(myfileDlg.GetPathName(),theFlags,(CString)"PCM");
      }
      catch (COleDispatchException *e)
      {
        if (e->m_scError == E_INVALIDARG)
          MessageBox((CString)"Invalid parameters.");

        else if (e->m_scError == S_FALSE)
          MessageBox((CString)"Necessary data not available.");

        else
          MessageBox(e->m_strDescription);
      }
    }
  }
  else
  {
    m_StartRecordMedia.SetWindowText((CString)"Start");

    this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxReceived)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxTransmitted)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxRecord)->EnableWindow(true);
    this->GetDlgItem(IDC_StartTransmitMedia)->EnableWindow(true);

    //Ends the ongoing recording and closes the file used for storing the media streams.
    m_AMC.StopRecordMedia();
  }
}

void CAudioSampleDlg::OnStartTransmitMedia() 
{
  CString aBtnCaption;
  m_BtnStartTransmitMedia.GetWindowText(aBtnCaption);

  if (aBtnCaption.Find((CString)"Start") != -1)
    //	if (!strcmp(aBtnCaption,"Start"))
  {
    // Starts playing the media stream.
    CFileDialog myfileDlg(TRUE, NULL, (CString)"*.wav",
      OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
      (CString)"Audio Clip (*.wav)| All Files (*.*)|*.*||");

    if(myfileDlg.DoModal ()==IDOK )
    {
      m_BtnStartTransmitMedia.SetWindowText((CString)"Stop");
      this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(false);
      this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(false);
      this->GetDlgItem(IDC_CheckBoxReceived)->EnableWindow(false);
      this->GetDlgItem(IDC_CheckBoxTransmitted)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(false);
      this->GetDlgItem(IDC_GroupBoxRecord)->EnableWindow(false);
      this->GetDlgItem(IDC_StartRecordMedia)->EnableWindow(false);


      try
      {
        CString str;

        // URL for transmitting audio to the server.
        m_AMC.put_AudioTransmitURL((CString)"http://" + m_ipText + (CString)"/axis-cgi/audio/transmit.cgi");

        // Starts to transmit the file to the server.
        m_AMC.StartTransmitMedia(myfileDlg.GetPathName(),0);
      }
      catch (COleDispatchException *e)
      {
        if (e->m_scError == E_INVALIDARG)
          MessageBox((CString)"Invalid parameters.");

        else if (e->m_scError == S_FALSE)
          MessageBox((CString)"Necessary data not available.");

        else
          MessageBox(e->m_strDescription);
      }
    }
  }
  else
  {
    m_BtnStartTransmitMedia.SetWindowText((CString)"Start");
    m_BtnStartTransmitMedia.SetWindowText((CString)"Start");
    this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxReceived)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxTransmitted)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxRecord)->EnableWindow(true);

    SetRecordButtonStatus();

    // Ends the ongoing file-transmission.
    m_AMC.StopTransmitMedia();
  }


}

void CAudioSampleDlg::OnRdbReceiveOn() 
{
  try
  {
    // The complete URL to an audio stream from an Axis product.
    m_AMC.put_AudioReceiveURL((CString)"http://" + m_ipText + (CString)"/axis-cgi/audio/receive.cgi");
    // Start stream
    m_AMC.AudioReceiveStart();
    m_CheckBoxReceived.EnableWindow(true);
    m_CheckBoxReceived.SetCheck(1);
    SetRecordButtonStatus();
  }
  catch (...)
  {
    // Handle errors in AMC_OnError event.
  }		
}

void CAudioSampleDlg::OnRdbReceiveOff() 
{
  try
  {
    // Stop stream
    m_AMC.AudioReceiveStop();

    m_CheckBoxReceived.EnableWindow(false);
    m_CheckBoxReceived.SetCheck(0);

    SetRecordButtonStatus();
  }
  catch (...)
  {
    // Handle errors in AMC_OnError event.
  }
}

void CAudioSampleDlg::OnRdbTransmitOn() 
{
  try
  {
    // URL for transmitting audio to the server.
    m_AMC.put_AudioTransmitURL((CString)"http://" + m_ipText + (CString)"/axis-cgi/audio/transmit.cgi");

    // Start stream
    m_AMC.AudioTransmitStart();

    m_CheckBoxTransmitted.EnableWindow(true);
    m_CheckBoxTransmitted.SetCheck(1);

    SetRecordButtonStatus();
  }
  catch (...)
  {
    // Handle errors in AMC_OnError event.
  }
}

void CAudioSampleDlg::OnRdbTransmitOff() 
{
  try
  {
    // Stop stream
    m_AMC.AudioTransmitStop();

    m_CheckBoxTransmitted.EnableWindow(false);
    m_CheckBoxTransmitted.SetCheck(0);
    SetRecordButtonStatus();
  }
  catch (...)
  {
    // Handle errors in AMC_OnError event.
  }
}

void CAudioSampleDlg::OnCheckBoxReceived() 
{
  SetRecordButtonStatus();
}

void CAudioSampleDlg::OnCheckBoxTransmitted() 
{
  SetRecordButtonStatus();
}

void CAudioSampleDlg::SetRecordButtonStatus() 
{
  if (m_CheckBoxTransmitted.GetCheck() == 1 || m_CheckBoxReceived.GetCheck() == 1)
    m_StartRecordMedia.EnableWindow(true);
  else 
    m_StartRecordMedia.EnableWindow(false);
}



void CAudioSampleDlg::OnConnect() 
{
  try
  {
    this->UpdateData();

    // This URL is used to retrieve audio configuration from an Axis device with audio capability.
    m_AMC.put_AudioConfigURL((CString)"http://" + m_ipText + (CString)"/axis-cgi/view/param.cgi?usergroup=anonymous&action=list&group=Audio,AudioSource");
    // To transmit audio in 16 kHz use this configuration URL instead (recording will not work)
    //m_AMC.put_AudioConfigURL((CString)"http://" + m_ipText + (CString)"/axis-cgi/view/param.cgi?usergroup=anonymous&action=list&group=Audio,AudioSource,Properties.Audio";

    CButton* myRadioButton;
    myRadioButton = (CButton*)GetDlgItem(IDC_RdbReceiveOn);

    // Start/Stop received media
    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioReceiveStart();
    else
      m_AMC.AudioReceiveStop();

    myRadioButton = (CButton*)GetDlgItem(IDC_RdbTransmitOn);

    // Start/Stop transmitted media
    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioTransmitStart();
    else
      m_AMC.AudioTransmitStop();

    this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmitFile)->EnableWindow(true);
    m_BtnStartTransmitMedia.EnableWindow(true);

  }
  catch (...)
  {
    // Handle errors in AMC_OnError event.
  }
}

void CAudioSampleDlg::OnBtnPlay() 
{
  CString aBtnCaption;
  m_BtnPlay.GetWindowText(aBtnCaption);

  if (aBtnCaption.Find((CString)"Start") != -1)

    //	if (!strcmp(aBtnCaption,"Start"))
  {
    // Starts playing the media stream.
    CFileDialog myfileDlg(TRUE, NULL, (CString)"*.wav",
      OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT,
      (CString)"Audio Clip (*.wav)| All Files (*.*)|*.*||");

    if(myfileDlg.DoModal ()==IDOK )
    {
      m_BtnPlay.SetWindowText((CString)"Stop");

      try
      {
        m_AMC.AudioReceiveStart();
        m_AMC.AudioTransmitStop();

        // Starts to transmit the file to the server.
        m_AMC.put_MediaFile(myfileDlg.GetPathName());
        m_AMC.Play();
      }
      catch (COleDispatchException *e)
      {
        if (e->m_scError == E_INVALIDARG)
          MessageBox((CString)"Invalid parameters.");

        else if (e->m_scError == S_FALSE)
          MessageBox((CString)"Necessary data not available.");

        else
          MessageBox(e->m_strDescription);
      }
    }
  }
  else
  {
    m_BtnPlay.SetWindowText((CString)"Start");

    // Ends the ongoing file-transmission.
    m_AMC.Stop();

    CButton* myRadioButton;
    myRadioButton = (CButton*)GetDlgItem(IDC_RdbReceiveOn);

    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioReceiveStart();

    myRadioButton = (CButton*)GetDlgItem(IDC_RdbTransmitOn);

    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioTransmitStart();
  }



}

BEGIN_EVENTSINK_MAP(CAudioSampleDlg, CDialog)
  //{{AFX_EVENTSINK_MAP(CAudioSampleDlg)
  ON_EVENT(CAudioSampleDlg, IDC_AXISMEDIACONTROL1, 1 /* OnError */, OnOnErrorAxismediacontrol1, VTS_I4 VTS_BSTR)
  //}}AFX_EVENTSINK_MAP
  ON_EVENT(CAudioSampleDlg, IDC_AXISMEDIACONTROL1, 8, CAudioSampleDlg::OnStatusChangeAxismediacontrol1, VTS_I4 VTS_I4)
END_EVENTSINK_MAP()



void CAudioSampleDlg::OnOnErrorAxismediacontrol1(long theErrorCode, LPCTSTR theErrorInfo) 
{
  MessageBox(theErrorInfo, (CString)"Error", MB_OK);	
}

void CAudioSampleDlg::OnStatusChangeAxismediacontrol1(long theNewStatus, long theOldStatus)
{
  if((theOldStatus & AMC_STATUS_FLAG_TRANSMIT_AUDIO_FILE) > 0 &&
    (theNewStatus & AMC_STATUS_FLAG_TRANSMIT_AUDIO_FILE) == 0)
  {
    // audio file transmit ended
    m_BtnStartTransmitMedia.SetWindowText((CString)"Start");
    this->GetDlgItem(IDC_RdbReceiveOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbReceiveOff)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOn)->EnableWindow(true);
    this->GetDlgItem(IDC_RdbTransmitOff)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxReceived)->EnableWindow(true);
    this->GetDlgItem(IDC_CheckBoxTransmitted)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxReceive)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxTransmit)->EnableWindow(true);
    this->GetDlgItem(IDC_GroupBoxRecord)->EnableWindow(true);

    SetRecordButtonStatus();
  }
  if ((theOldStatus & AMC_STATUS_FLAG_PLAYING) > 0 &&
    (theNewStatus & AMC_STATUS_FLAG_PLAYING) == 0)
  {
    // audio file playback ended
    m_BtnPlay.SetWindowText((CString)"Start");

    CButton* myRadioButton;
    myRadioButton = (CButton*)GetDlgItem(IDC_RdbReceiveOn);

    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioReceiveStart();

    myRadioButton = (CButton*)GetDlgItem(IDC_RdbTransmitOn);

    if (myRadioButton->GetCheck() == 1)
      m_AMC.AudioTransmitStart();
  }
}
