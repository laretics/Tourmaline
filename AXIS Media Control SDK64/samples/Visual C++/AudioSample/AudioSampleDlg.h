// AudioSampleDlg.h : header file
//

#pragma once
#include "axismediacontrol.h"

/////// AMC audio status flags ///////
#define AMC_STATUS_FLAG_PLAYING                 2L
#define AMC_STATUS_FLAG_OPENING_RECEIVE_AUDIO   65536L
#define AMC_STATUS_FLAG_OPENING_TRANSMIT_AUDIO  131072L
#define AMC_STATUS_FLAG_RECEIVE_AUDIO           262144L
#define AMC_STATUS_FLAG_TRANSMIT_AUDIO          524288L
#define AMC_STATUS_FLAG_TRANSMIT_AUDIO_FILE     1048576L
#define AMC_STATUS_FLAG_RECORDING_AUDIO         2097152L



// CAudioSampleDlg dialog
class CAudioSampleDlg : public CDialog
{
// Construction
public:
	CAudioSampleDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_AUDIOSAMPLE_DIALOG };
	CButton	m_BtnPlay;
	CButton	m_BtnStartTransmitMedia;
	CButton	m_GroupBoxReceive;
	CButton	m_StartRecordMedia;
	CButton	m_CheckBoxTransmitted;
	CButton	m_CheckBoxReceived;
	CString	m_ipText;
	//}}AFX_DATA

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnStartRecordMedia();
	afx_msg void OnStartTransmitMedia();
	afx_msg void OnRdbReceiveOn();
	afx_msg void OnRdbReceiveOff();
	afx_msg void OnRdbTransmitOn();
	afx_msg void OnRdbTransmitOff();
	afx_msg void OnCheckBoxReceived();
	afx_msg void OnCheckBoxTransmitted();
	afx_msg void OnConnect();
	afx_msg void OnBtnPlay();
	afx_msg void OnOnErrorAxismediacontrol1(long theErrorCode, LPCTSTR theErrorInfo);
	DECLARE_EVENTSINK_MAP()
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
public:
  CAxisMediaControl m_AMC;
  
private:
	void SetRecordButtonStatus();
  
public:
  void OnStatusChangeAxismediacontrol1(long theNewStatus, long theOldStatus);
};
