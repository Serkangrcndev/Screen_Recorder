Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.IO
Imports System.Threading.Tasks

Namespace UI

    Public Class InstallerWizard
        Inherits Form

        Private _currentStep As Integer = 0
        Private _totalSteps As Integer = 4

        ' Header
        Private _lblTitle As Label
        Private _lblSubtitle As Label
        Private _picIcon As PictureBox

        ' Content
        Private _contentPanel As Panel
        Private _progressBar As ProgressBar
        Private _lblStatus As Label
        Private _chkShortcut As CheckBox
        Private _chkDesktop As CheckBox

        ' Buttons
        Private _btnBack As Button
        Private _btnNext As Button
        Private _btnCancel As Button

        ' State
        Private _progress As New Progress(Of String)(AddressOf OnProgressReport)
        Public Property IsInstallComplete As Boolean = False

        Public Sub New()
            InitializeComponent()
            ShowStep(0)
        End Sub

        Private Sub InitializeComponent()
            ' Form settings
            Text = "Screen Recorder Kurulumu"
            Size = New Size(550, 400)
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Icon = CreateFilmIcon()

            ' Header panel
            Dim headerPanel = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 65,
                .BackColor = Color.White
            }

            ' Icon
            _picIcon = New PictureBox() With {
                .Location = New Point(15, 12),
                .Size = New Size(40, 40),
                .BackColor = Color.Transparent
            }
            AddHandler _picIcon.Paint, AddressOf DrawFilmIcon
            headerPanel.Controls.Add(_picIcon)

            ' Title
            _lblTitle = New Label() With {
                .Text = "Screen Recorder Kurulumu",
                .Location = New Point(65, 10),
                .Size = New Size(450, 25),
                .Font = New Font("Segoe UI", 14, FontStyle.Regular),
                .ForeColor = Color.FromArgb(0, 51, 153),
                .BackColor = Color.Transparent
            }
            headerPanel.Controls.Add(_lblTitle)

            ' Subtitle
            _lblSubtitle = New Label() With {
                .Text = "Kurulum sihirbazı Screen Recorder ürününü yüklerken lütfen bekleyin.",
                .Location = New Point(65, 38),
                .Size = New Size(450, 20),
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.FromArgb(80, 80, 80),
                .BackColor = Color.Transparent
            }
            headerPanel.Controls.Add(_lblSubtitle)

            ' Separator line
            Dim separator = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 2,
                .BackColor = Color.FromArgb(0, 128, 0),
                .Location = New Point(0, 65),
                .Margin = Padding.Empty
            }

            ' Content panel
            _contentPanel = New Panel() With {
                .Location = New Point(0, 67),
                .Size = New Size(550, 240),
                .BackColor = Color.White
            }

            ' Status label
            _lblStatus = New Label() With {
                .Text = "Hoş geldiniz! Kuruluma başlamak için İleri'ye tıklayın.",
                .Location = New Point(40, 30),
                .Size = New Size(470, 20),
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.Black,
                .BackColor = Color.Transparent
            }
            _contentPanel.Controls.Add(_lblStatus)

            ' Progress bar
            _progressBar = New ProgressBar() With {
                .Location = New Point(40, 160),
                .Size = New Size(470, 20),
                .Style = ProgressBarStyle.Continuous,
                .Minimum = 0,
                .Maximum = 100,
                .Value = 0
            }
            _contentPanel.Controls.Add(_progressBar)

            ' Bottom separator
            Dim bottomSeparator = New Panel() With {
                .Location = New Point(0, 307),
                .Size = New Size(550, 1),
                .BackColor = Color.FromArgb(200, 200, 200)
            }

            ' Buttons
            _btnCancel = CreateButton("İptal", New Point(445, 325), New Size(80, 25))
            AddHandler _btnCancel.Click, AddressOf OnCancelClick

            _btnNext = CreateButton("İleri >", New Point(355, 325), New Size(80, 25))
            _btnNext.TabIndex = 0
            AddHandler _btnNext.Click, AddressOf OnNextClick

            _btnBack = CreateButton("< Geri", New Point(265, 325), New Size(80, 25))
            _btnBack.Enabled = False
            AddHandler _btnBack.Click, AddressOf OnBackClick

            ' Add controls
            Controls.Add(headerPanel)
            Controls.Add(separator)
            Controls.Add(_contentPanel)
            Controls.Add(bottomSeparator)
            Controls.Add(_btnCancel)
            Controls.Add(_btnNext)
            Controls.Add(_btnBack)
        End Sub

        Private Function CreateButton(text As String, location As Point, size As Size) As Button
            Dim btn = New Button() With {
                .Text = text,
                .Location = location,
                .Size = size,
                .FlatStyle = FlatStyle.System,
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.Black,
                .BackColor = Color.FromArgb(240, 240, 240)
            }
            Return btn
        End Function

        Private Function CreateFilmIcon() As Icon
            Try
                Dim bitmap = New Bitmap(32, 32)
                Using g = Graphics.FromImage(bitmap)
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    g.Clear(Color.Transparent)

                    ' Film reel
                    Using brush = New SolidBrush(Color.FromArgb(0, 128, 0))
                        g.FillEllipse(brush, 4, 4, 24, 24)
                    End Using

                    ' Film strip holes
                    Using brush = New SolidBrush(Color.White)
                        g.FillEllipse(brush, 12, 8, 4, 4)
                        g.FillEllipse(brush, 12, 20, 4, 4)
                    End Using

                    ' Center hole
                    Using brush = New SolidBrush(Color.FromArgb(0, 100, 0))
                        g.FillEllipse(brush, 14, 14, 4, 4)
                    End Using
                End Using

                Return Icon.FromHandle(bitmap.GetHicon())
            Catch
                Return SystemIcons.Application
            End Try
        End Function

        Private Sub DrawFilmIcon(sender As Object, e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            ' Film strip icon
            Using brush = New SolidBrush(Color.FromArgb(0, 128, 0))
                ' Main film rectangle
                g.FillRectangle(brush, 8, 10, 24, 20)
            End Using

            ' Film holes top
            Using holeBrush = New SolidBrush(Color.White)
                For i = 0 To 3
                    g.FillRectangle(holeBrush, 10 + i * 6, 12, 3, 3)
                    g.FillRectangle(holeBrush, 10 + i * 6, 25, 3, 3)
                Next
            End Using

            ' Center play area
            Using centerBrush = New SolidBrush(Color.FromArgb(0, 100, 0))
                g.FillRectangle(centerBrush, 12, 16, 16, 8)
            End Using
        End Sub

        Private Sub ShowStep(stepIndex As Integer)
            _currentStep = stepIndex

            Select Case stepIndex
                Case 0
                    ShowWelcomeStep()
                Case 1
                    ShowLicenseStep()
                Case 2
                    ShowInstallStep()
                Case 3
                    ShowCompleteStep()
            End Select

            UpdateButtons()
        End Sub

        Private Sub ShowWelcomeStep()
            _lblTitle.Text = "Screen Recorder Kurulumu"
            _lblSubtitle.Text = "Kurulum sihirbazı Screen Recorder ürününü yüklerken lütfen bekleyin."
            _lblStatus.Text = "Hoş geldiniz! Screen Recorder uygulamasını kurmak üzeresiniz." & vbCrLf & vbCrLf & _
                             "Bu kurulum şunları içerir:" & vbCrLf & _
                             "• FFmpeg video encoder" & vbCrLf & _
                             "• Screen Recorder uygulaması" & vbCrLf & _
                             "• Gerekli sistem dosyaları" & vbCrLf & vbCrLf & _
                             "Devam etmek için İleri'ye tıklayın."
            _progressBar.Value = 0
            _progressBar.Visible = True
        End Sub

        Private Sub ShowLicenseStep()
            _lblTitle.Text = "Lisans Anlaşması"
            _lblSubtitle.Text = "Devam etmeden önce lisans anlaşmasını okuyun."
            _lblStatus.Text = "MIT Lisansı" & vbCrLf & vbCrLf & _
                             "Copyright (c) 2024 Screen Recorder" & vbCrLf & vbCrLf & _
                             "Bu yazılım ücretsizdir ve açık kaynak kodludur." & vbCrLf & _
                             "Yazılımı kullanmak, kopyalamak, değiştirmek, birleştirmek," & vbCrLf & _
                             "yayınlamak, dağıtmak, alt lisanslamak ve/veya yazılımın" & vbCrLf & _
                             "kopyalarını satmak mümkündür."
            _progressBar.Value = 10
        End Sub

        Private Sub ShowInstallStep()
            _lblTitle.Text = "Kurulum"
            _lblSubtitle.Text = "Screen Recorder yükleniyor..."
            _lblStatus.Text = "FFmpeg indiriliyor ve kuruluyor... (Yaklaşık 140 MB)" & vbCrLf & vbCrLf & _
                             "Lütfen bekleyin, bu işlem birkaç dakika sürebilir."
            _btnNext.Enabled = False
            _btnBack.Enabled = False
            _btnCancel.Enabled = False

            ' Start installation
            Task.Run(Async Function()
                         Dim success = Await Core.FFmpegInstaller.DownloadAndInstallFFmpeg(_progress, New System.Threading.CancellationToken())
                         Invoke(New Action(Sub()
                                               If success Then
                                                   _progressBar.Value = 100
                                                   _lblStatus.Text = "Kurulum tamamlandı!" & vbCrLf & vbCrLf & "FFmpeg başarıyla kuruldu."
                                                   ShowStep(3)
                                               Else
                                                   _lblStatus.Text = "Kurulum başarısız!" & vbCrLf & vbCrLf & "Lütfen internet bağlantınızı kontrol edin."
                                                   _btnCancel.Enabled = True
                                                   _btnCancel.Text = "Kapat"
                                               End If
                                           End Sub))
                     End Function)
        End Sub

        Private Sub ShowCompleteStep()
            _lblTitle.Text = "Kurulum Tamamlandı"
            _lblSubtitle.Text = "Screen Recorder başarıyla kuruldu."
            _lblStatus.Text = "Tebrikler! Screen Recorder kurulumu tamamlandı." & vbCrLf & vbCrLf & _
                             "Başlat menüsünden uygulamayı çalıştırabilirsiniz."
            _progressBar.Value = 100
            _btnNext.Text = "Bitir"
            _btnNext.Enabled = True
            _btnBack.Enabled = False
            _btnCancel.Visible = False
            IsInstallComplete = True
        End Sub

        Private Sub UpdateButtons()
            Select Case _currentStep
                Case 0
                    _btnBack.Enabled = False
                    _btnNext.Text = "İleri >"
                    _btnCancel.Text = "İptal"
                    _progressBar.Value = 0
                Case 1
                    _btnBack.Enabled = True
                    _btnNext.Text = "İleri >"
                    _progressBar.Value = 10
                Case 2
                    _btnBack.Enabled = False
                    _btnNext.Text = "İleri >"
                    _progressBar.Visible = True
                Case 3
                    _btnBack.Enabled = False
                    _btnNext.Text = "Bitir"
                    _btnCancel.Visible = False
                    _progressBar.Value = 100
            End Select
        End Sub

        Private Sub OnProgressReport(status As String)
            If InvokeRequired Then
                Invoke(New Action(Of String)(AddressOf OnProgressReport), status)
                Return
            End If

            If status.Contains("%") Then
                ' Parse percentage
                Try
                    Dim percentText = status.Split("%"c)(0).Split(" "c).Last()
                    Dim percent = Integer.Parse(percentText)
                    _progressBar.Value = Math.Min(percent, 100)
                Catch
                    ' Ignore parse errors
                End Try
            End If

            If _currentStep = 2 Then
                _lblStatus.Text = status & vbCrLf & vbCrLf & "Lütfen bekleyin..."
            End If
        End Sub

        Private Sub OnNextClick(sender As Object, e As EventArgs)
            If _currentStep < _totalSteps - 1 Then
                ShowStep(_currentStep + 1)
            Else
                Close()
            End If
        End Sub

        Private Sub OnBackClick(sender As Object, e As EventArgs)
            If _currentStep > 0 Then
                ShowStep(_currentStep - 1)
            End If
        End Sub

        Private Sub OnCancelClick(sender As Object, e As EventArgs)
            If MessageBox.Show("Kurulumu iptal etmek istediğinize emin misiniz?", "İptal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                IsInstallComplete = False
                Close()
            End If
        End Sub

    End Class

End Namespace
