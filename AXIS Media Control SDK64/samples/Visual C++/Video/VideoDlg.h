// VideoDlg.h : header file
//

#pragma once
#include "axismediacontrol.h"


// CVideoDlg dialog
class CVideoDlg : public CDialog
{
// Construction
public:
	CVideoDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_VIDEO_DIALOG };
	CString	m_File;
	CString m_ipText;
    CString m_type;
    CString m_user;
    CString m_pass;

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
  afx_msg void OnPaint();
  afx_msg void OnMouseMove(UINT nFlags, CPoint point);
	afx_msg HCURSOR OnQueryDragIcon();
		
	afx_msg void OnPlayLive();
	afx_msg void OnStopPlay();
	afx_msg void OnSnapshot();
	afx_msg void OnBrowse();
	afx_msg void OnStartRecord();
	afx_msg void OnStopRecord();
	afx_msg void OnPlayFile();

	DECLARE_MESSAGE_MAP()
public:
  CAxisMediaControl m_AMC;
  DECLARE_EVENTSINK_MAP()
  void OnStatusChangeAxismediacontrol1(long theNewStatus, long theOldStatus);
  void OnErrorAxismediacontrol1(long theErrorCode, LPCTSTR theErrorInfo);
};
