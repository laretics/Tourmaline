// PTZDlg.h : header file
//

#pragma once
#include "axismediacontrol.h"


// CPTZDlg dialog
class CPTZDlg : public CDialog
{
// Construction
public:
	CPTZDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_PTZ_DIALOG };
	CString m_ipText;

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	afx_msg void OnConnect();

	DECLARE_MESSAGE_MAP()
public:
  CAxisMediaControl m_AMC;
};
