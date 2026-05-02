Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.IO

Namespace UI

    Public Class SettingsForm
        Inherits Form

        Private _settings As Models.RecordingSettings
        Private _hotkeyManager As Core.GlobalHotkeyManager

        Private _txtOutputPath As TextBox
        Private _btnBrowse As Button
        Private _cmbCodec As ComboBox
        Private _cmbFrameRate As ComboBox
        Private _trackQuality As TrackBar
        Private _lblQualityValue As Label
        Private _chkSystemAudio As CheckBox
        Private _chkMicrophone As CheckBox
        Private _cmbMicrophone As ComboBox
        Private _chkAutoDate As CheckBox
        Private _chkAutoProject As CheckBox
        Private _txtProjectName As TextBox
        Private _txtHotkeyStart As TextBox
        Private _txtHotkeyPause As TextBox
        Private _txtHotkeyPrivacy As TextBox
        Private _btnSave As Button
        Private _btnCancel As Button

        Private _navPanel As Panel
        Private _contentPanel As Panel
        Private _navButtons As New List(Of Button)

        Public Property SavedSettings As Models.RecordingSettings

        Public Sub New(settings As Models.RecordingSettings, hotkeyManager As Core.GlobalHotkeyManager)
            _settings = settings
            _hotkeyManager = hotkeyManager
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Text = "Ayarlar"
            Size = New Size(900, 650)
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.FromArgb(32, 32, 32)
            ForeColor = Color.White

            ' Main container
            Dim mainPanel = New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(0),
                .BackColor = Color.Transparent
            }

            ' Left Navigation Panel
            CreateNavigationPanel()

            ' Right Content Panel
            CreateContentPanel()

            mainPanel.Controls.Add(_contentPanel)
            mainPanel.Controls.Add(_navPanel)
            Controls.Add(mainPanel)

            ' Bottom buttons
            CreateBottomButtons()
        End Sub

        Private Structure Margins
            Public Left As Integer
            Public Right As Integer
            Public Top As Integer
            Public Bottom As Integer
        End Structure

        <System.Runtime.InteropServices.DllImport("dwmapi.dll")>
        Private Shared Sub DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer)
        End Sub

        <System.Runtime.InteropServices.DllImport("dwmapi.dll")>
        Private Shared Sub DwmExtendFrameIntoClientArea(hwnd As IntPtr, ByRef margins As Margins)
        End Sub

        Private Sub CreateNavigationPanel()
            _navPanel = New Panel() With {
                .Location = New Point(0, 0),
                .Size = New Size(220, 580),
                .BackColor = Color.FromArgb(40, 40, 40)
            }

            ' Title
            Dim lblTitle = New Label() With {
                .Text = "Ayarlar",
                .Location = New Point(20, 20),
                .Size = New Size(180, 35),
                .Font = New Font("Segoe UI Variable", 18, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent
            }
            _navPanel.Controls.Add(lblTitle)

            ' Navigation buttons
            Dim tabs = New String() {"Genel", "Video", "Ses", "Kısayollar", "Gelişmiş"}
            Dim icons = New String() {"⚙", "🎬", "🎤", "⌨", "🔧"}
            Dim yPos = 70

            For i = 0 To tabs.Length - 1
                Dim btn = CreateNavButton(tabs(i), icons(i), yPos)
                _navButtons.Add(btn)
                _navPanel.Controls.Add(btn)
                yPos += 50
            Next

            UpdateNavSelection("Genel")
        End Sub

        Private Function CreateNavButton(text As String, icon As String, y As Integer) As Button
            Dim btn = New Button() With {
                .Text = "  " & icon & "   " & text,
                .Location = New Point(10, y),
                .Size = New Size(200, 44),
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Regular),
                .ForeColor = Color.FromArgb(180, 180, 180),
                .BackColor = Color.Transparent,
                .Cursor = Cursors.Hand,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(10, 0, 0, 0)
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 55)

            AddHandler btn.Click, Sub(s, e)
                                      UpdateNavSelection(text)
                                      ShowTabContent(text)
                                  End Sub

            Return btn
        End Function

        Private Sub UpdateNavSelection(selected As String)
            For Each btn In _navButtons
                If btn.Text.Contains(selected) Then
                    btn.BackColor = Color.FromArgb(0, 95, 184)
                    btn.ForeColor = Color.White
                    btn.Font = New Font("Segoe UI", 12, FontStyle.Bold)
                Else
                    btn.BackColor = Color.Transparent
                    btn.ForeColor = Color.FromArgb(180, 180, 180)
                    btn.Font = New Font("Segoe UI", 12, FontStyle.Regular)
                End If
            Next
        End Sub

        Private Sub CreateContentPanel()
            _contentPanel = New Panel() With {
                .Location = New Point(220, 0),
                .Size = New Size(680, 580),
                .BackColor = Color.Transparent,
                .AutoScroll = True,
                .Padding = New Padding(30)
            }

            ShowTabContent("Genel")
        End Sub

        Private Sub ShowTabContent(tabName As String)
            _contentPanel.Controls.Clear()

            Select Case tabName
                Case "Genel"
                    ShowGeneralTab()
                Case "Video"
                    ShowVideoTab()
                Case "Ses"
                    ShowAudioTab()
                Case "Kısayollar"
                    ShowHotkeysTab()
                Case "Gelişmiş"
                    ShowAdvancedTab()
            End Select
        End Sub

        Private Function CreateCard(title As String, y As Integer, height As Integer) As Panel
            Dim card = New Panel() With {
                .Location = New Point(30, y),
                .Size = New Size(600, height),
                .BackColor = Color.FromArgb(45, 45, 45)
            }

            ' Apply rounded corners via Region
            Using path = RoundedRectangle.Create(New Rectangle(0, 0, card.Width, card.Height), 12)
                card.Region = New Region(path)
            End Using

            ' Card title
            Dim lblTitle = New Label() With {
                .Text = title,
                .Location = New Point(20, 15),
                .Size = New Size(560, 28),
                .Font = New Font("Segoe UI Variable", 14, FontStyle.Bold),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent
            }
            card.Controls.Add(lblTitle)

            Return card
        End Function

        Private Sub ShowGeneralTab()
            ' Output Path Card
            Dim card1 = CreateCard("Dosya Konumu", 20, 140)

            _txtOutputPath = New TextBox() With {
                .Location = New Point(20, 55),
                .Size = New Size(430, 30),
                .Font = New Font("Segoe UI", 10),
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle
            }

            _btnBrowse = CreateModernButton("Gözat", New Point(460, 52), New Size(120, 34))
            AddHandler _btnBrowse.Click, AddressOf OnBrowseClick

            card1.Controls.Add(_txtOutputPath)
            card1.Controls.Add(_btnBrowse)
            _contentPanel.Controls.Add(card1)

            ' Organization Card
            Dim card2 = CreateCard("Organizasyon", 180, 180)

            _chkAutoDate = CreateModernCheckBox("Tarihe göre klasörle", 20, 60)
            _chkAutoProject = CreateModernCheckBox("Projeye göre klasörle", 20, 95)

            Dim lblProject = New Label() With {
                .Text = "Proje Adı:",
                .Location = New Point(20, 130),
                .Size = New Size(100, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }

            _txtProjectName = New TextBox() With {
                .Location = New Point(130, 128),
                .Size = New Size(200, 28),
                .Font = New Font("Segoe UI", 10),
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle
            }

            ' Load values
            If _settings IsNot Nothing Then
                _txtOutputPath.Text = _settings.OutputPath
                _chkAutoDate.Checked = _settings.AutoOrganizeByDate
                _chkAutoProject.Checked = _settings.AutoOrganizeByProject
                _txtProjectName.Text = _settings.ProjectName
            End If

            card2.Controls.Add(_chkAutoDate)
            card2.Controls.Add(_chkAutoProject)
            card2.Controls.Add(lblProject)
            card2.Controls.Add(_txtProjectName)
            _contentPanel.Controls.Add(card2)
        End Sub

        Private Sub ShowVideoTab()
            ' Video Settings Card
            Dim card = CreateCard("Video Ayarları", 20, 280)

            ' Codec
            Dim lblCodec = New Label() With {
                .Text = "Codec:",
                .Location = New Point(20, 55),
                .Size = New Size(100, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }

            _cmbCodec = New ComboBox() With {
                .Location = New Point(130, 52),
                .Size = New Size(200, 28),
                .Font = New Font("Segoe UI", 10),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            _cmbCodec.Items.AddRange(New String() {"H.264 (Hızlı)", "H.265 (Verimli)", "Lossless (Kayıpsız)"})

            ' Frame Rate
            Dim lblFrameRate = New Label() With {
                .Text = "FPS:",
                .Location = New Point(20, 100),
                .Size = New Size(100, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }

            _cmbFrameRate = New ComboBox() With {
                .Location = New Point(130, 97),
                .Size = New Size(200, 28),
                .Font = New Font("Segoe UI", 10),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            _cmbFrameRate.Items.AddRange(New String() {"30 FPS", "60 FPS", "120 FPS"})

            ' Quality
            Dim lblQuality = New Label() With {
                .Text = "Kalite:",
                .Location = New Point(20, 145),
                .Size = New Size(100, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }

            _trackQuality = New TrackBar() With {
                .Location = New Point(130, 140),
                .Size = New Size(350, 45),
                .Minimum = 0,
                .Maximum = 51,
                .TickFrequency = 5
            }

            _lblQualityValue = New Label() With {
                .Location = New Point(490, 145),
                .Size = New Size(50, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.White,
                .BackColor = Color.Transparent
            }

            AddHandler _trackQuality.ValueChanged, Sub(s, e) _lblQualityValue.Text = _trackQuality.Value.ToString()

            ' Load values
            If _settings IsNot Nothing Then
                _cmbCodec.SelectedIndex = CInt(_settings.VideoCodec)
                _cmbFrameRate.SelectedIndex = If(_settings.FrameRate = 30, 0, If(_settings.FrameRate = 60, 1, 2))
                _trackQuality.Value = _settings.VideoQuality
                _lblQualityValue.Text = _settings.VideoQuality.ToString()
            End If

            card.Controls.Add(lblCodec)
            card.Controls.Add(_cmbCodec)
            card.Controls.Add(lblFrameRate)
            card.Controls.Add(_cmbFrameRate)
            card.Controls.Add(lblQuality)
            card.Controls.Add(_trackQuality)
            card.Controls.Add(_lblQualityValue)
            _contentPanel.Controls.Add(card)
        End Sub

        Private Sub ShowAudioTab()
            Dim card = CreateCard("Ses Ayarları", 20, 220)

            _chkSystemAudio = CreateModernCheckBox("Sistem sesini kaydet", 20, 60)
            _chkMicrophone = CreateModernCheckBox("Mikrofonu kaydet", 20, 95)

            Dim lblMic = New Label() With {
                .Text = "Mikrofon:",
                .Location = New Point(20, 135),
                .Size = New Size(100, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }

            _cmbMicrophone = New ComboBox() With {
                .Location = New Point(130, 132),
                .Size = New Size(300, 28),
                .Font = New Font("Segoe UI", 10),
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }

            Dim devices = Core.AudioCapture.GetAudioDevices()
            If devices.Count = 0 Then
                _cmbMicrophone.Items.Add("Mikrofon bulunamadı")
                _cmbMicrophone.SelectedIndex = 0
                _cmbMicrophone.Enabled = False
            Else
                _cmbMicrophone.Items.AddRange(devices.ToArray())
                _cmbMicrophone.SelectedIndex = 0
            End If

            ' Load values
            If _settings IsNot Nothing Then
                _chkSystemAudio.Checked = _settings.RecordSystemAudio
                _chkMicrophone.Checked = _settings.RecordMicrophone
                If _settings.MicrophoneDevice IsNot Nothing Then
                    Dim idx = _cmbMicrophone.Items.IndexOf(_settings.MicrophoneDevice)
                    If idx >= 0 Then _cmbMicrophone.SelectedIndex = idx
                End If
            End If

            card.Controls.Add(_chkSystemAudio)
            card.Controls.Add(_chkMicrophone)
            card.Controls.Add(lblMic)
            card.Controls.Add(_cmbMicrophone)
            _contentPanel.Controls.Add(card)
        End Sub

        Private Sub ShowHotkeysTab()
            Dim card = CreateCard("Klavye Kısayolları", 20, 220)

            _txtHotkeyStart = CreateHotkeyBox(New Point(180, 60))
            _txtHotkeyPause = CreateHotkeyBox(New Point(180, 100))
            _txtHotkeyPrivacy = CreateHotkeyBox(New Point(180, 140))

            card.Controls.Add(CreateHotkeyLabel("Kayıt Başlat/Durdur:", 20, 60))
            card.Controls.Add(_txtHotkeyStart)
            card.Controls.Add(CreateHotkeyLabel("Duraklat/Devam:", 20, 100))
            card.Controls.Add(_txtHotkeyPause)
            card.Controls.Add(CreateHotkeyLabel("Gizli Mod:", 20, 140))
            card.Controls.Add(_txtHotkeyPrivacy)

            ' Load values
            If _settings IsNot Nothing Then
                _txtHotkeyStart.Text = _settings.Hotkeys.StartStopRecording.ToString()
                _txtHotkeyPause.Text = _settings.Hotkeys.PauseResumeRecording.ToString()
                _txtHotkeyPrivacy.Text = _settings.Hotkeys.TogglePrivacyMode.ToString()
            End If

            AddHandler _txtHotkeyStart.KeyDown, AddressOf OnHotkeyKeyDown
            AddHandler _txtHotkeyPause.KeyDown, AddressOf OnHotkeyKeyDown
            AddHandler _txtHotkeyPrivacy.KeyDown, AddressOf OnHotkeyKeyDown

            _contentPanel.Controls.Add(card)
        End Sub

        Private Sub ShowAdvancedTab()
            Dim card = CreateCard("Gelişmiş Ayarlar", 20, 150)

            Dim lblInfo = New Label() With {
                .Text = "Gelişmiş ayarlar için config.json dosyasını düzenleyebilirsiniz.",
                .Location = New Point(20, 60),
                .Size = New Size(560, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(180, 180, 180),
                .BackColor = Color.Transparent
            }

            Dim lblPath = New Label() With {
                .Text = $"Konum: {Path.Combine(Application.StartupPath, "config.json")}",
                .Location = New Point(20, 95),
                .Size = New Size(560, 40),
                .Font = New Font("Segoe UI", 9),
                .ForeColor = Color.FromArgb(150, 150, 150),
                .BackColor = Color.Transparent
            }

            card.Controls.Add(lblInfo)
            card.Controls.Add(lblPath)
            _contentPanel.Controls.Add(card)
        End Sub

        Private Function CreateModernButton(text As String, location As Point, size As Size) As Button
            Dim btn = New Button() With {
                .Text = text,
                .Location = location,
                .Size = size,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 10, FontStyle.Regular),
                .ForeColor = Color.White,
                .BackColor = Color.FromArgb(0, 95, 184),
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 212)
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 78, 152)

            ' Apply rounded corners
            Using path = RoundedRectangle.Create(New Rectangle(0, 0, btn.Width, btn.Height), 8)
                btn.Region = New Region(path)
            End Using

            Return btn
        End Function

        Private Function CreateModernCheckBox(text As String, x As Integer, y As Integer) As CheckBox
            Return New CheckBox() With {
                .Text = text,
                .Location = New Point(x, y),
                .Size = New Size(250, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(220, 220, 220),
                .BackColor = Color.Transparent
            }
        End Function

        Private Function CreateHotkeyLabel(text As String, x As Integer, y As Integer) As Label
            Return New Label() With {
                .Text = text,
                .Location = New Point(x, y),
                .Size = New Size(150, 25),
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent
            }
        End Function

        Private Function CreateHotkeyBox(location As Point) As TextBox
            Return New TextBox() With {
                .Location = location,
                .Size = New Size(150, 28),
                .Font = New Font("Segoe UI", 10),
                .ReadOnly = True,
                .BackColor = Color.FromArgb(55, 55, 55),
                .ForeColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .TextAlign = HorizontalAlignment.Center
            }
        End Function

        Private Sub CreateBottomButtons()
            _btnSave = CreateModernButton("Kaydet", New Point(620, 590), New Size(120, 40))
            _btnSave.Font = New Font("Segoe UI", 11, FontStyle.Bold)
            AddHandler _btnSave.Click, AddressOf OnSaveClick

            _btnCancel = CreateModernButton("İptal", New Point(490, 590), New Size(120, 40))
            _btnCancel.BackColor = Color.FromArgb(60, 60, 60)
            _btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80)
            _btnCancel.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 50)
            AddHandler _btnCancel.Click, Sub(s, e) Close()

            Controls.Add(_btnSave)
            Controls.Add(_btnCancel)
        End Sub

        Private Sub OnHotkeyKeyDown(sender As Object, e As KeyEventArgs)
            Dim txt = CType(sender, TextBox)
            txt.Text = e.KeyCode.ToString()
            e.Handled = True
            e.SuppressKeyPress = True
        End Sub

        Private Sub OnBrowseClick(sender As Object, e As EventArgs)
            Using fbd = New FolderBrowserDialog()
                fbd.Description = "Kayıt klasörü seçin"
                If Directory.Exists(_txtOutputPath.Text) Then
                    fbd.SelectedPath = _txtOutputPath.Text
                End If
                If fbd.ShowDialog() = DialogResult.OK Then
                    _txtOutputPath.Text = fbd.SelectedPath
                End If
            End Using
        End Sub

        Private Sub OnSaveClick(sender As Object, e As EventArgs)
            _settings.OutputPath = _txtOutputPath.Text
            _settings.VideoCodec = CType(_cmbCodec.SelectedIndex, Models.VideoCodec)
            _settings.FrameRate = CInt(_cmbFrameRate.Text.Split(" "c)(0))
            _settings.VideoQuality = _trackQuality.Value
            _settings.RecordSystemAudio = _chkSystemAudio.Checked
            _settings.RecordMicrophone = _chkMicrophone.Checked
            _settings.MicrophoneDevice = _cmbMicrophone.SelectedItem?.ToString()
            _settings.AutoOrganizeByDate = _chkAutoDate.Checked
            _settings.AutoOrganizeByProject = _chkAutoProject.Checked
            _settings.ProjectName = _txtProjectName.Text

            ' Parse and save hotkeys
            Try
                If Not String.IsNullOrEmpty(_txtHotkeyStart.Text) Then
                    _settings.Hotkeys.StartStopRecording = CType([Enum].Parse(GetType(Keys), _txtHotkeyStart.Text, True), Keys)
                End If
                If Not String.IsNullOrEmpty(_txtHotkeyPause.Text) Then
                    _settings.Hotkeys.PauseResumeRecording = CType([Enum].Parse(GetType(Keys), _txtHotkeyPause.Text, True), Keys)
                End If
                If Not String.IsNullOrEmpty(_txtHotkeyPrivacy.Text) Then
                    _settings.Hotkeys.TogglePrivacyMode = CType([Enum].Parse(GetType(Keys), _txtHotkeyPrivacy.Text, True), Keys)
                End If
            Catch
                ' Ignore parse errors
            End Try

            SavedSettings = _settings
            DialogResult = DialogResult.OK
            Close()
        End Sub

    End Class

End Namespace
