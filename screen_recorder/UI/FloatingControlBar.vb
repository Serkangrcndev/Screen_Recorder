Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace UI

    ' Enums at namespace level
    Public Enum RoundButtonType
        [Default]
        Record
        [Stop]
        Pause
        Play
        Settings
        Minimize
        Close
    End Enum

    Public Enum ToggleIconType
        [Default]
        Shield
        Pen
        Play
        Crop      ' Area selection for privacy
        NoEntry   ' Do not disturb mode
        Mic       ' Microphone toggle
    End Enum

    Public Class FloatingControlBar
        Inherits Form

        Private _isDragging As Boolean = False
        Private _dragStartPoint As Point
        Private _isExpanded As Boolean = True
        Private _recordingTime As TimeSpan = TimeSpan.Zero
        Private _recordingTimer As Timer
        Private _isRecording As Boolean = False
        Private _isPaused As Boolean = False
        Private _lastTickTime As DateTime

        Private _btnRecord As RoundButton
        Private _btnPause As RoundButton
        Private _btnStop As RoundButton
        Private _btnSettings As RoundButton
        Private _btnMinimize As RoundButton
        Private _btnClose As RoundButton
        Private _btnPrivacy As RoundToggleButton
        Private _btnMic As RoundToggleButton
        Private _lblTimer As Label
        Private _lblStatus As Label
        Private _tooltip As ToolTip

        Private _formRadius As Integer = 16

        Public Event RecordClicked(sender As Object, e As EventArgs)
        Public Event StopClicked(sender As Object, e As EventArgs)
        Public Event PauseClicked(sender As Object, e As EventArgs)
        Public Event SettingsClicked(sender As Object, e As EventArgs)
        Public Event PrivacyModeToggled(sender As Object, e As EventArgs)
        Public Event MicToggled(sender As Object, e As EventArgs)
        Public Event MinimizeClicked(sender As Object, e As EventArgs)

        Public Property IsRecording As Boolean
            Get
                Return _isRecording
            End Get
            Set(value As Boolean)
                _isRecording = value
                UpdateUIState()
            End Set
        End Property

        Public Property IsPaused As Boolean
            Get
                Return _isPaused
            End Get
            Set(value As Boolean)
                _isPaused = value
                UpdateUIState()
            End Set
        End Property

        Public Property RecordingTime As TimeSpan
            Get
                Return _recordingTime
            End Get
            Set(value As TimeSpan)
                _recordingTime = value
                UpdateTimerDisplay()
            End Set
        End Property

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            InitializeComponent()
            InitializeDrag()
        End Sub

        Private Sub InitializeComponent()
            FormBorderStyle = FormBorderStyle.None
            StartPosition = FormStartPosition.Manual
            Size = New Size(400, 70)
            Location = New Point(
                (Screen.PrimaryScreen.WorkingArea.Width - Width) \ 2,
                Screen.PrimaryScreen.WorkingArea.Height - Height - 20)
            
            TopMost = True
            ShowInTaskbar = False
            BackColor = Color.FromArgb(45, 45, 48)
            
            ' Left Panel - Recording Controls (More compact)
            _btnRecord = New RoundButton() With {
                .Size = New Size(40, 40),
                .Location = New Point(12, 15),
                .ButtonType = RoundButtonType.Record,
                .ToolTipText = "Kayıt Başlat"
            }
            AddHandler _btnRecord.Click, Sub(s, e) RaiseEvent RecordClicked(Me, e)
            
            _btnPause = New RoundButton() With {
                .Size = New Size(36, 36),
                .Location = New Point(14, 17),
                .ButtonType = RoundButtonType.Pause,
                .Visible = False,
                .ToolTipText = "Duraklat"
            }
            AddHandler _btnPause.Click, Sub(s, e) RaiseEvent PauseClicked(Me, e)
            
            _btnStop = New RoundButton() With {
                .Size = New Size(36, 36),
                .Location = New Point(56, 17),
                .ButtonType = RoundButtonType.Stop,
                .Visible = False,
                .ToolTipText = "Durdur"
            }
            AddHandler _btnStop.Click, Sub(s, e) RaiseEvent StopClicked(Me, e)
            
            ' Timer - Centered vertically next to record - wider for milliseconds
            _lblTimer = New Label() With {
                .Location = New Point(62, 16),
                .Size = New Size(105, 24),
                .Font = New Font("Segoe UI Variable", 14, FontStyle.Bold),
                .ForeColor = Color.White,
                .Text = "00:00:00:00",
                .TextAlign = ContentAlignment.MiddleLeft
            }
            
            ' Status - Centered below timer
            _lblStatus = New Label() With {
                .Location = New Point(62, 40),
                .Size = New Size(105, 14),
                .Font = New Font("Segoe UI Variable", 8),
                .ForeColor = Color.FromArgb(180, 180, 180),
                .Text = "Hazır",
                .TextAlign = ContentAlignment.MiddleLeft
            }
            
            ' Right Panel - Tools (Evenly spaced) - moved right for wider form
            ' Privacy = Area Selection / Masking
            _btnPrivacy = New RoundToggleButton() With {
                .Size = New Size(34, 34),
                .Location = New Point(210, 18),
                .IconType = ToggleIconType.Shield,
                .ToolTipText = "Gizli Alan Ekle"
            }
            AddHandler _btnPrivacy.Click, Sub(s, e) RaiseEvent PrivacyModeToggled(Me, e)
            
            ' Microphone Toggle
            _btnMic = New RoundToggleButton() With {
                .Size = New Size(34, 34),
                .Location = New Point(254, 18),
                .IconType = ToggleIconType.Mic,
                .ToolTipText = "Mikrofon (Açık)",
                .IsChecked = True
            }
            AddHandler _btnMic.Click, Sub(s, e) RaiseEvent MicToggled(Me, e)
            
            _btnSettings = New RoundButton() With {
                .Size = New Size(34, 34),
                .Location = New Point(298, 18),
                .ButtonType = RoundButtonType.Settings,
                .ToolTipText = "Ayarlar"
            }
            AddHandler _btnSettings.Click, Sub(s, e) RaiseEvent SettingsClicked(Me, e)
            
            ' Close and Minimize Buttons (Side by side at top right) - swapped positions
            ' Minimize (-) on left, Close (X) on right
            _btnMinimize = New RoundButton() With {
                .Size = New Size(16, 16),
                .Location = New Point(352, 4),
                .ButtonType = RoundButtonType.Minimize,
                .ToolTipText = "Küçült"
            }
            AddHandler _btnMinimize.Click, Sub(s, e) ToggleMinimize()
            
            _btnClose = New RoundButton() With {
                .Size = New Size(16, 16),
                .Location = New Point(376, 4),
                .ButtonType = RoundButtonType.Close,
                .ToolTipText = "Kapat"
            }
            AddHandler _btnClose.Click, Sub(s, e) Application.Exit()

            ' Setup tooltips
            SetupTooltips()
            
            ' Add controls (first = bottom, last = top)
            ' Left side: Record controls (added first - bottom layer)
            Controls.Add(_btnRecord)
            Controls.Add(_lblTimer)
            Controls.Add(_lblStatus)
            
            ' Center: Stop/Pause (added after record - middle layer)
            ' These will be hidden when recording starts
            Controls.Add(_btnPause)
            Controls.Add(_btnStop)
            
            ' Right side: Tools (added last - top layer)
            Controls.Add(_btnPrivacy)
            Controls.Add(_btnMic)
            Controls.Add(_btnSettings)
            
            ' Top-right: Window controls (always on top)
            Controls.Add(_btnMinimize)
            Controls.Add(_btnClose)
            
            ' Timer - 16ms interval for smooth 60fps animation
            _recordingTimer = New Timer()
            _recordingTimer.Interval = 16
            AddHandler _recordingTimer.Tick, AddressOf OnRecordingTimerTick
        End Sub

        Private Sub InitializeDrag()
            AddHandler MouseDown, AddressOf OnMouseDownDrag
            AddHandler MouseMove, AddressOf OnMouseMoveDrag
            AddHandler MouseUp, AddressOf OnMouseUpDrag
        End Sub

        Private Sub OnMouseDownDrag(sender As Object, e As MouseEventArgs)
            If e.Button = MouseButtons.Left Then
                _isDragging = True
                _dragStartPoint = e.Location
            End If
        End Sub

        Private Sub OnMouseMoveDrag(sender As Object, e As MouseEventArgs)
            If _isDragging Then
                Location = New Point(
                    Left + e.X - _dragStartPoint.X,
                    Top + e.Y - _dragStartPoint.Y)
            End If
        End Sub

        Private Sub OnMouseUpDrag(sender As Object, e As MouseEventArgs)
            _isDragging = False
        End Sub

        Private Sub SetupTooltips()
            _tooltip = New ToolTip()
            _tooltip.InitialDelay = 500
            _tooltip.ReshowDelay = 100
            _tooltip.AutoPopDelay = 3000
            _tooltip.ShowAlways = True
            _tooltip.BackColor = Color.FromArgb(50, 50, 50)
            _tooltip.ForeColor = Color.White
            _tooltip.IsBalloon = False

            ' Set tooltips for all buttons
            If Not String.IsNullOrEmpty(_btnRecord.ToolTipText) Then _tooltip.SetToolTip(_btnRecord, _btnRecord.ToolTipText)
            If Not String.IsNullOrEmpty(_btnPause.ToolTipText) Then _tooltip.SetToolTip(_btnPause, _btnPause.ToolTipText)
            If Not String.IsNullOrEmpty(_btnStop.ToolTipText) Then _tooltip.SetToolTip(_btnStop, _btnStop.ToolTipText)
            If Not String.IsNullOrEmpty(_btnSettings.ToolTipText) Then _tooltip.SetToolTip(_btnSettings, _btnSettings.ToolTipText)
            If Not String.IsNullOrEmpty(_btnMinimize.ToolTipText) Then _tooltip.SetToolTip(_btnMinimize, _btnMinimize.ToolTipText)
            If Not String.IsNullOrEmpty(_btnClose.ToolTipText) Then _tooltip.SetToolTip(_btnClose, _btnClose.ToolTipText)
            If Not String.IsNullOrEmpty(_btnPrivacy.ToolTipText) Then _tooltip.SetToolTip(_btnPrivacy, _btnPrivacy.ToolTipText)
            If Not String.IsNullOrEmpty(_btnMic.ToolTipText) Then _tooltip.SetToolTip(_btnMic, _btnMic.ToolTipText)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.Half
            
            Dim rect As New Rectangle(0, 0, Width - 1, Height - 1)
            Using path = CreateRoundedPath(rect, _formRadius)
                Using brush = New SolidBrush(BackColor)
                    g.FillPath(brush, path)
                End Using
                
                Using pen = New Pen(Color.FromArgb(80, 80, 80), 1)
                    g.DrawPath(pen, path)
                End Using
            End Using
            
            Using regionPath = CreateRoundedPath(New Rectangle(0, 0, Width, Height), _formRadius)
                Region = New Region(regionPath)
            End Using
            
            ' Draw separator lines
            Using pen = New Pen(Color.FromArgb(60, 60, 60), 1)
                ' Line between Record and Timer (adjusted for compact record button)
                g.DrawLine(pen, 58, 20, 58, 50)
                ' Line between Timer area and Tools
                g.DrawLine(pen, 192, 15, 192, 55)
                ' Lines between the 3 right panel buttons (Privacy, Drawing, Settings)
                g.DrawLine(pen, 247, 22, 247, 48)  ' Between Privacy and Drawing
                g.DrawLine(pen, 291, 22, 291, 48)  ' Between Drawing and Settings
            End Using
        End Sub

        Private Function CreateRoundedPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter = radius * 2
            
            path.StartFigure()
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90)
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90)
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90)
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90)
            path.CloseFigure()
            
            Return path
        End Function

        Private Sub OnRecordingTimerTick(sender As Object, e As EventArgs)
            If Not _isPaused Then
                Dim now = DateTime.Now
                Dim elapsed = now - _lastTickTime
                _lastTickTime = now
                _recordingTime = _recordingTime.Add(elapsed)
                
                UpdateTimerDisplay()
                
                If _btnPause.Visible Then
                    _btnPause.AnimationProgress = CSng((_recordingTime.TotalMilliseconds Mod 2000) / 2000.0)
                    _btnPause.Invalidate()
                End If
            End If
        End Sub

        Private Sub UpdateTimerDisplay()
            ' Format: HH:MM:SS:MS (milliseconds)
            _lblTimer.Text = String.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}", 
                _recordingTime.Hours, 
                _recordingTime.Minutes, 
                _recordingTime.Seconds,
                _recordingTime.Milliseconds \ 10)  ' Show 2 digits of milliseconds
        End Sub

        Private Sub UpdateUIState()
            If _isRecording Then
                _btnRecord.Visible = False
                _btnPause.Visible = True
                _btnStop.Visible = True
                
                ' Shift labels right to make room for both Pause and Stop buttons
                _lblTimer.Location = New Point(98, 16)
                _lblStatus.Location = New Point(98, 40)
                
                _lblStatus.Text = If(_isPaused, "Duraklatıldı", "Kaydediliyor")
                _lblStatus.ForeColor = If(_isPaused, Color.Orange, Color.FromArgb(255, 107, 107))
                
                If _isPaused Then
                    _btnPause.ButtonType = RoundButtonType.Play
                    _btnPause.ToolTipText = "Devam Et"
                    _recordingTimer.Stop()
                Else
                    _btnPause.ButtonType = RoundButtonType.Pause
                    _btnPause.ToolTipText = "Duraklat"
                    _lastTickTime = DateTime.Now
                    _recordingTimer.Start()
                End If
                
                If _tooltip IsNot Nothing Then
                    _tooltip.SetToolTip(_btnPause, _btnPause.ToolTipText)
                End If
                _btnPause.Invalidate()
            Else
                _btnRecord.Visible = True
                _btnPause.Visible = False
                _btnStop.Visible = False
                
                ' Restore label positions
                _lblTimer.Location = New Point(62, 16)
                _lblStatus.Location = New Point(62, 40)
                
                _recordingTime = TimeSpan.Zero
                _btnPause.AnimationProgress = 0.0F
                UpdateTimerDisplay()
                _lblStatus.Text = "Hazır"
                _lblStatus.ForeColor = Color.FromArgb(180, 180, 180)
                _recordingTimer.Stop()
                _btnPrivacy.IsChecked = False
                _btnMic.IsChecked = True
            End If
        End Sub

        Private Sub ToggleMinimize()
            _isExpanded = Not _isExpanded
            
            ' Hide/show all controls except minimize button
            For Each ctrl In Controls
                If ctrl IsNot _btnMinimize Then
                    CType(ctrl, Control).Visible = _isExpanded
                End If
            Next
            
            ' Always show minimize button
            _btnMinimize.Visible = True
            
            ' Restore correct button visibility based on recording state
            If _isExpanded Then
                _btnRecord.Visible = Not _isRecording
                _btnPause.Visible = _isRecording
                _btnStop.Visible = _isRecording
                _lblTimer.Visible = True
                _lblStatus.Visible = True
            End If
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            _recordingTimer?.Stop()
            _recordingTimer?.Dispose()
            MyBase.OnFormClosing(e)
        End Sub
    End Class

    ' Custom Round Button
    Public Class RoundButton
        Inherits Button

        Private _isHovered As Boolean = False
        Private _isPressed As Boolean = False
        Private _radius As Integer = 10

        Public Property ButtonType As RoundButtonType = RoundButtonType.Default
        Public Property ToolTipText As String = ""
        Public Property AnimationProgress As Single = 0.0F

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            Size = New Size(40, 40)
            Cursor = Cursors.Hand
            BackColor = Color.Transparent
        End Sub

        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            UpdateRegion()
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            UpdateRegion()
        End Sub

        Private Sub UpdateRegion()
            If Width > 0 AndAlso Height > 0 Then
                Using path = CreateRoundedPath(New Rectangle(0, 0, Width, Height), _radius)
                    Region = New Region(path)
                End Using
            End If
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _isHovered = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _isHovered = False
            _isPressed = False
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            _isPressed = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            _isPressed = False
            Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            ' Don't call MyBase.OnPaint to avoid Windows default border
            
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.Half
            
            ' Full button size
            Dim rect = New Rectangle(0, 0, Width, Height)
            
            ' Always same as form background - no hover effect at all
            Using path = CreateRoundedPath(rect, _radius)
                Using brush = New SolidBrush(Color.FromArgb(45, 45, 48))
                    g.FillPath(brush, path)
                End Using
            End Using
            
            DrawIcon(g, rect)
        End Sub

        Private Sub DrawIcon(g As Graphics, rect As Rectangle)
            Dim centerX = rect.Left + rect.Width \ 2
            Dim centerY = rect.Top + rect.Height \ 2
            Dim iconColor = Color.FromArgb(220, 220, 220)
            
            Select Case ButtonType
                Case RoundButtonType.Record
                    ' Glassmorphism style record button - glowing red
                    Using brush = New SolidBrush(Color.FromArgb(255, 65, 65))
                        g.FillEllipse(brush, centerX - 10, centerY - 10, 20, 20)
                    End Using
                    ' Inner glow
                    Using brush = New SolidBrush(Color.FromArgb(255, 90, 90))
                        g.FillEllipse(brush, centerX - 6, centerY - 6, 12, 12)
                    End Using
                    
                Case RoundButtonType.Stop
                    Using brush = New SolidBrush(Color.FromArgb(255, 65, 65))
                        g.FillRectangle(brush, centerX - 8, centerY - 8, 16, 16)
                    End Using
                    
                Case RoundButtonType.Pause
                    Using brush = New SolidBrush(Color.White)
                        g.FillRectangle(brush, centerX - 6, centerY - 8, 4, 16)
                        g.FillRectangle(brush, centerX + 2, centerY - 8, 4, 16)
                    End Using
                    
                    ' Draw animated red border effect
                    If AnimationProgress > 0 Then
                        Using animPen = New Pen(Color.FromArgb(255, 65, 65), 2)
                            Dim sweepAngle = AnimationProgress * 360.0F
                            g.DrawArc(animPen, 2, 2, Width - 5, Height - 5, -90, sweepAngle)
                        End Using
                    End If
                    
                Case RoundButtonType.Play
                    Using brush = New SolidBrush(Color.White)
                        Dim points = New Point() {
                            New Point(centerX - 4, centerY - 6),
                            New Point(centerX + 6, centerY),
                            New Point(centerX - 4, centerY + 6)
                        }
                        g.FillPolygon(brush, points)
                    End Using
                    
                Case RoundButtonType.Settings
                    ' Settings/Sliders Icon - more recognizable
                    Using pen = New Pen(iconColor, 2)
                        ' Three horizontal lines with different lengths (sliders)
                        ' Top slider - long
                        g.DrawLine(pen, centerX - 9, centerY - 6, centerX + 3, centerY - 6)
                        g.DrawLine(pen, centerX + 3, centerY - 8, centerX + 3, centerY - 4)
                        ' Middle slider - medium  
                        g.DrawLine(pen, centerX - 9, centerY, centerX - 2, centerY)
                        g.DrawLine(pen, centerX - 2, centerY - 2, centerX - 2, centerY + 2)
                        ' Bottom slider - short
                        g.DrawLine(pen, centerX - 9, centerY + 6, centerX + 5, centerY + 6)
                        g.DrawLine(pen, centerX + 5, centerY + 4, centerX + 5, centerY + 8)
                    End Using
                    
                Case RoundButtonType.Minimize
                    ' Simple minus icon
                    Using pen = New Pen(Color.FromArgb(200, 200, 200), 2)
                        g.DrawLine(pen, centerX - 4, centerY, centerX + 4, centerY)
                    End Using
                    
                Case RoundButtonType.Close
                    ' X icon for close
                    Using pen = New Pen(Color.FromArgb(255, 100, 100), 2)
                        g.DrawLine(pen, centerX - 4, centerY - 4, centerX + 4, centerY + 4)
                        g.DrawLine(pen, centerX + 4, centerY - 4, centerX - 4, centerY + 4)
                    End Using
            End Select
        End Sub

        Private Function CreateRoundedPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d = radius * 2
            
            path.StartFigure()
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            
            Return path
        End Function
    End Class

    ' Custom Round Toggle Button
    Public Class RoundToggleButton
        Inherits Button

        Private _isHovered As Boolean = False
        Private _isPressed As Boolean = False
        Private _isChecked As Boolean = False
        Private _radius As Integer = 8

        Public Property IconType As ToggleIconType = ToggleIconType.Default
        Public Property ToolTipText As String = ""

        Public Property IsChecked As Boolean
            Get
                Return _isChecked
            End Get
            Set(value As Boolean)
                _isChecked = value
                Invalidate()
            End Set
        End Property

        Public Property IsPaused As Boolean
            Get
                Return _isChecked
            End Get
            Set(value As Boolean)
                _isChecked = value
                Invalidate()
            End Set
        End Property

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            Size = New Size(36, 36)
            Cursor = Cursors.Hand
            BackColor = Color.Transparent
        End Sub

        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            UpdateRegion()
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            UpdateRegion()
        End Sub

        Private Sub UpdateRegion()
            If Width > 0 AndAlso Height > 0 Then
                Using path = CreateRoundedPath(New Rectangle(0, 0, Width, Height), _radius)
                    Region = New Region(path)
                End Using
            End If
        End Sub

        Protected Overrides Sub OnClick(e As EventArgs)
            _isChecked = Not _isChecked
            MyBase.OnClick(e)
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e)
            _isHovered = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _isHovered = False
            _isPressed = False
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            _isPressed = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            _isPressed = False
            Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            ' Don't call MyBase.OnPaint to avoid Windows default border
            
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.Half
            
            ' Full button size
            Dim rect = New Rectangle(0, 0, Width, Height)
            
            ' Only blue when checked, otherwise same as form - no hover effect
            Dim bgColor As Color
            If _isChecked Then
                bgColor = Color.FromArgb(0, 122, 204)  ' Blue when active
            Else
                bgColor = Color.FromArgb(45, 45, 48)  ' Same as form background
            End If
            
            Using path = CreateRoundedPath(rect, _radius)
                Using brush = New SolidBrush(bgColor)
                    g.FillPath(brush, path)
                End Using
            End Using
            
            DrawIcon(g, rect)
        End Sub

        Private Sub DrawIcon(g As Graphics, rect As Rectangle)
            Dim centerX = rect.Left + rect.Width \ 2
            Dim centerY = rect.Top + rect.Height \ 2
            Dim iconColor = If(_isChecked, Color.White, Color.FromArgb(220, 220, 220))
            
            Select Case IconType
                Case ToggleIconType.Shield
                    ' Privacy Mode Icon - Lock symbol (more recognizable)
                    Using brush = New SolidBrush(iconColor)
                        ' Lock body (rectangle)
                        g.FillRectangle(brush, centerX - 6, centerY - 2, 12, 9)
                        ' Lock shackle (arc)
                        Using shacklePen = New Pen(iconColor, 2)
                            g.DrawArc(shacklePen, centerX - 5, centerY - 8, 10, 8, 180, 180)
                        End Using
                        ' Keyhole
                        Using keyholeBrush = New SolidBrush(Color.FromArgb(45, 45, 48))
                            g.FillEllipse(keyholeBrush, centerX - 2, centerY + 1, 4, 4)
                        End Using
                    End Using
                    
                Case ToggleIconType.Mic
                    Using brush = New SolidBrush(iconColor)
                        Using pen = New Pen(iconColor, 2)
                            ' Mic body
                            g.FillRectangle(brush, centerX - 3, centerY - 6, 6, 10)
                            g.FillPie(brush, centerX - 3, centerY - 9, 6, 6, 180, 180)
                            g.FillPie(brush, centerX - 3, centerY - 1, 6, 6, 0, 180)
                            
                            ' Mic stand/curve
                            g.DrawArc(pen, centerX - 6, centerY - 2, 12, 10, 0, 180)
                            g.DrawLine(pen, centerX, centerY + 8, centerX, centerY + 12)
                            g.DrawLine(pen, centerX - 4, centerY + 12, centerX + 4, centerY + 12)
                            
                            ' If muted (not checked), draw a slash
                            If Not _isChecked Then
                                Using redPen = New Pen(Color.FromArgb(255, 100, 100), 2)
                                    g.DrawLine(redPen, centerX - 8, centerY - 8, centerX + 8, centerY + 8)
                                End Using
                            End If
                        End Using
                    End Using
                    
                Case ToggleIconType.Pen
                    ' Draw a pen/pencil icon
                    Using penBrush = New SolidBrush(iconColor)
                        Using linePen = New Pen(iconColor, 2)
                            ' Draw pencil body (diagonal line)
                            g.DrawLine(linePen, centerX - 4, centerY + 4, centerX + 4, centerY - 4)
                            ' Pencil tip
                            Dim points = New Point() {
                                New Point(centerX - 6, centerY + 6),
                                New Point(centerX - 4, centerY + 4),
                                New Point(centerX - 2, centerY + 6)
                            }
                            g.FillPolygon(penBrush, points)
                        End Using
                    End Using
                    
                Case ToggleIconType.Crop
                    ' Area Selection / Crop Icon (like first image)
                    Using pen = New Pen(iconColor, 2)
                        ' Draw the crop frame - corners extending outward
                        Dim frameSize = 10
                        ' Top-left corner
                        g.DrawLine(pen, centerX - frameSize, centerY - frameSize + 5, centerX - frameSize, centerY - frameSize)
                        g.DrawLine(pen, centerX - frameSize, centerY - frameSize, centerX - frameSize + 5, centerY - frameSize)
                        ' Top-right corner  
                        g.DrawLine(pen, centerX + frameSize - 5, centerY - frameSize, centerX + frameSize, centerY - frameSize)
                        g.DrawLine(pen, centerX + frameSize, centerY - frameSize, centerX + frameSize, centerY - frameSize + 5)
                        ' Bottom-left corner
                        g.DrawLine(pen, centerX - frameSize, centerY + frameSize - 5, centerX - frameSize, centerY + frameSize)
                        g.DrawLine(pen, centerX - frameSize, centerY + frameSize, centerX - frameSize + 5, centerY + frameSize)
                        ' Bottom-right corner
                        g.DrawLine(pen, centerX + frameSize - 5, centerY + frameSize, centerX + frameSize, centerY + frameSize)
                        g.DrawLine(pen, centerX + frameSize, centerY + frameSize, centerX + frameSize, centerY + frameSize - 5)
                    End Using
                    ' Inner rectangle
                    Using pen = New Pen(Color.FromArgb(150, 150, 150), 1)
                        g.DrawRectangle(pen, centerX - 5, centerY - 5, 10, 10)
                    End Using
                    
                Case ToggleIconType.NoEntry
                    ' Do Not Disturb / No Entry Icon (like second image)
                    Using brush = New SolidBrush(iconColor)
                        ' Circle outline
                        Using pen = New Pen(iconColor, 2)
                            g.DrawEllipse(pen, centerX - 9, centerY - 9, 18, 18)
                            ' Diagonal line
                            g.DrawLine(pen, centerX - 6, centerY - 6, centerX + 6, centerY + 6)
                        End Using
                    End Using
                    
                Case ToggleIconType.Play
                    Using brush = New SolidBrush(iconColor)
                        Dim points = New Point() {
                            New Point(centerX - 4, centerY - 6),
                            New Point(centerX + 6, centerY),
                            New Point(centerX - 4, centerY + 6)
                        }
                        g.FillPolygon(brush, points)
                    End Using
                    
                Case Else
                    Using brush = New SolidBrush(iconColor)
                        g.FillEllipse(brush, centerX - 6, centerY - 6, 12, 12)
                    End Using
            End Select
        End Sub

        Private Function CreateRoundedPath(rect As Rectangle, radius As Integer) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim d = radius * 2
            
            path.StartFigure()
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            
            Return path
        End Function
    End Class

    Module ControlExtensions
        <System.Runtime.CompilerServices.Extension()>
        Public Sub SetToolTip(control As Control, text As String)
            Dim toolTip = New ToolTip()
            toolTip.SetToolTip(control, text)
        End Sub
    End Module

End Namespace
