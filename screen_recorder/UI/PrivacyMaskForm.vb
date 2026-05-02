Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    Public Class PrivacyMaskForm
        Inherits Form

        Private _masks As New List(Of MaskDefinition)()
        Private _isDefiningMask As Boolean = False
        Private _maskStartPoint As Point
        Private _currentMask As MaskDefinition
        Private _selectedMaskType As Models.MaskType = Models.MaskType.Blur
        Private _previewBitmap As Bitmap

        Public Event MasksDefined(sender As Object, e As MasksDefinedEventArgs)

        Public Class MaskDefinition
            Public Property Bounds As Rectangle
            Public Property MaskType As Models.MaskType
            Public Property Intensity As Integer = 20
        End Class

        Public Property SelectedMaskType As Models.MaskType
            Get
                Return _selectedMaskType
            End Get
            Set(value As Models.MaskType)
                _selectedMaskType = value
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
            InitializeMaskSurface()
        End Sub

        Private Sub InitializeComponent()
            FormBorderStyle = FormBorderStyle.None
            WindowState = FormWindowState.Maximized
            StartPosition = FormStartPosition.Manual
            Bounds = Screen.PrimaryScreen.Bounds
            BackColor = Color.Black
            Opacity = 0.3
            ShowInTaskbar = False
            TopMost = True
            Cursor = Cursors.Cross
            DoubleBuffered = True
        End Sub

        Private Sub InitializeMaskSurface()
            _previewBitmap = New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
            Using g = Graphics.FromImage(_previewBitmap)
                g.Clear(Color.Transparent)
            End Using
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Button = MouseButtons.Left Then
                _isDefiningMask = True
                _maskStartPoint = e.Location
                _currentMask = New MaskDefinition With {
                    .Bounds = New Rectangle(e.Location, Size.Empty),
                    .MaskType = _selectedMaskType
                }
            ElseIf e.Button = MouseButtons.Right Then
                RemoveMaskAt(e.Location)
            End If
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)

            If _isDefiningMask AndAlso _currentMask IsNot Nothing Then
                Dim x = Math.Min(_maskStartPoint.X, e.X)
                Dim y = Math.Min(_maskStartPoint.Y, e.Y)
                Dim width = Math.Abs(e.X - _maskStartPoint.X)
                Dim height = Math.Abs(e.Y - _maskStartPoint.Y)

                _currentMask.Bounds = New Rectangle(x, y, width, height)
                Invalidate()
            Else
                Dim mask = GetMaskAt(e.Location)
                Cursor = If(mask IsNot Nothing, Cursors.Hand, Cursors.Cross)
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)

            If _isDefiningMask AndAlso _currentMask IsNot Nothing Then
                _isDefiningMask = False

                If _currentMask.Bounds.Width > 10 AndAlso _currentMask.Bounds.Height > 10 Then
                    _masks.Add(_currentMask)
                    RaiseEvent MasksDefined(Me, New MasksDefinedEventArgs(_masks.ToList()))
                End If

                _currentMask = Nothing
                Invalidate()
            End If
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)

            If e.KeyCode = Keys.Escape Then
                Close()
            ElseIf e.KeyCode = Keys.Delete Then
                ClearAllMasks()
            ElseIf e.KeyCode = Keys.S Then
                FinalizeMasks()
            End If
        End Sub

        Private Sub RemoveMaskAt(location As Point)
            Dim maskToRemove = GetMaskAt(location)
            If maskToRemove IsNot Nothing Then
                _masks.Remove(maskToRemove)
                RaiseEvent MasksDefined(Me, New MasksDefinedEventArgs(_masks.ToList()))
                Invalidate()
            End If
        End Sub

        Private Function GetMaskAt(location As Point) As MaskDefinition
            For i As Integer = _masks.Count - 1 To 0 Step -1
                If _masks(i).Bounds.Contains(location) Then
                    Return _masks(i)
                End If
            Next
            Return Nothing
        End Function

        Private Sub ClearAllMasks()
            _masks.Clear()
            RaiseEvent MasksDefined(Me, New MasksDefinedEventArgs(_masks.ToList()))
            Invalidate()
        End Sub

        Private Sub FinalizeMasks()
            RaiseEvent MasksDefined(Me, New MasksDefinedEventArgs(_masks.ToList(), True))
            Close()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias

            Dim screenWidth = Screen.PrimaryScreen.Bounds.Width
            Dim screenHeight = Screen.PrimaryScreen.Bounds.Height

            Using darkBrush = New SolidBrush(Color.FromArgb(180, 0, 0, 0))
                g.FillRectangle(darkBrush, 0, 0, screenWidth, screenHeight)
            End Using

            For Each mask In _masks
                DrawMask(g, mask)
            Next

            If _isDefiningMask AndAlso _currentMask IsNot Nothing Then
                DrawMask(g, _currentMask, True)
            End If

            DrawInstructions(g)
        End Sub

        Private Sub DrawMask(g As Graphics, mask As MaskDefinition, Optional isPreview As Boolean = False)
            Dim rect = mask.Bounds

            If isPreview Then
                Using brush = New SolidBrush(Color.FromArgb(100, 255, 0, 0))
                    g.FillRectangle(brush, rect)
                End Using
                Using pen = New Pen(Color.Red, 2)
                    g.DrawRectangle(pen, rect)
                End Using
            Else
                Using brush = New SolidBrush(Color.White)
                    g.FillRectangle(brush, rect)
                End Using
                Using pen = New Pen(Color.Red, 2)
                    pen.DashStyle = DashStyle.Dash
                    g.DrawRectangle(pen, rect)
                End Using

                Dim label = GetMaskTypeLabel(mask.MaskType)
                Using font = New Font("Segoe UI", 10, FontStyle.Bold)
                    Dim textSize = g.MeasureString(label, font)
                    Dim textRect = New Rectangle(rect.X, rect.Y - 20, CInt(textSize.Width) + 10, 20)

                    Using bgBrush = New SolidBrush(Color.Red)
                        g.FillRectangle(bgBrush, textRect)
                    End Using

                    TextRenderer.DrawText(g, label, font, textRect, Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
                End Using
            End If
        End Sub

        Private Function GetMaskTypeLabel(maskType As Models.MaskType) As String
            Select Case maskType
                Case Models.MaskType.Blur
                    Return "BLUR"
                Case Models.MaskType.SolidBlack
                    Return "BLACK"
                Case Models.MaskType.SolidWhite
                    Return "WHITE"
                Case Models.MaskType.Pixelate
                    Return "PIXELATE"
                Case Else
                    Return "MASK"
            End Select
        End Function

        Private Sub DrawInstructions(g As Graphics)
            Dim instructions = "Sol Tık: Alan Seç | Sağ Tık: Alan Kaldır | Delete: Tümünü Temizle | Esc: Kapat | S: Kaydet"
            Using font = New Font("Segoe UI", 12, FontStyle.Regular)
                Dim screenWidth = Screen.PrimaryScreen.Bounds.Width
                Dim textSize = g.MeasureString(instructions, font)
                Dim x = (screenWidth - CInt(textSize.Width)) \ 2

                TextRenderer.DrawText(g, instructions, font, New Point(x, 30), Color.White, TextFormatFlags.Left)
            End Using
        End Sub

        Public Function GetMasks() As List(Of MaskDefinition)
            Return _masks.ToList()
        End Function

        Protected Overrides Sub OnFormClosing(e As System.Windows.Forms.FormClosingEventArgs)
            _previewBitmap?.Dispose()
            MyBase.OnFormClosing(e)
        End Sub

    End Class

    Public Class MasksDefinedEventArgs
        Inherits EventArgs

        Public ReadOnly Property Masks As List(Of PrivacyMaskForm.MaskDefinition)
        Public ReadOnly Property IsFinalized As Boolean

        Public Sub New(masks As List(Of PrivacyMaskForm.MaskDefinition), Optional isFinalized As Boolean = False)
            Me.Masks = masks
            Me.IsFinalized = isFinalized
        End Sub
    End Class

End Namespace
