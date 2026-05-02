Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    Public Class OverlayDrawingForm
        Inherits Form

        Private _isDrawing As Boolean = False
        Private _lastPoint As Point
        Private _currentTool As DrawingTool = DrawingTool.Pen
        Private _currentColor As Color = Color.Red
        Private _currentSize As Integer = 3
        Private _drawingBitmap As Bitmap
        Private _drawingGraphics As Graphics
        Private _shapes As New List(Of DrawingShape)()
        Private _currentShape As DrawingShape

        ' Toolbar controls
        Private _toolPanel As Panel
        Private _colorPanel As Panel
        Private _btnClear As Button
        Private _btnClose As Button
        Private _lblInfo As Label
        Private _sizeSlider As TrackBar
        Private _selectedToolButton As Button

        Public Event ShapeDrawn(sender As Object, e As ShapeDrawnEventArgs)

        Public Enum DrawingTool
            Pen
            Line
            Rectangle
            Ellipse
            Arrow
            Text
            Highlighter
            Eraser
        End Enum

        Public Property CurrentTool As DrawingTool
            Get
                Return _currentTool
            End Get
            Set(value As DrawingTool)
                _currentTool = value
                UpdateCursor()
            End Set
        End Property

        Public Property CurrentColor As Color
            Get
                Return _currentColor
            End Get
            Set(value As Color)
                _currentColor = value
            End Set
        End Property

        Public Property CurrentSize As Integer
            Get
                Return _currentSize
            End Get
            Set(value As Integer)
                _currentSize = value
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
            InitializeDrawingSurface()
        End Sub

        Private Sub InitializeComponent()
            FormBorderStyle = FormBorderStyle.None
            WindowState = FormWindowState.Maximized
            StartPosition = FormStartPosition.Manual
            Bounds = Screen.PrimaryScreen.Bounds
            BackColor = Color.White
            TransparencyKey = Color.White
            Opacity = 1.0
            ShowInTaskbar = False
            TopMost = True
            Cursor = Cursors.Cross
            DoubleBuffered = True

            ' Create toolbar
            CreateToolbar()
        End Sub

        Private Sub InitializeDrawingSurface()
            _drawingBitmap = New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
            _drawingGraphics = Graphics.FromImage(_drawingBitmap)
            _drawingGraphics.SmoothingMode = SmoothingMode.AntiAlias
            _drawingGraphics.Clear(Color.Transparent)
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)

            If e.KeyCode = Keys.Escape Then
                Close()
            ElseIf e.KeyCode = Keys.C Then
                ClearDrawing()
            End If
        End Sub

        Private Sub CreateToolbar()
            ' Main tool panel at bottom
            _toolPanel = New Panel() With {
                .Location = New Point(0, Height - 100),
                .Size = New Size(Width, 100),
                .BackColor = Color.FromArgb(240, 45, 45, 48),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            }

            ' Tool buttons
            Dim tools = New (Icon As String, Tool As DrawingTool, Name As String)() {
                ("✏️", DrawingTool.Pen, "Kalem"),
                ("📏", DrawingTool.Line, "Çizgi"),
                ("▭", DrawingTool.Rectangle, "Dikdörtgen"),
                ("⭕", DrawingTool.Ellipse, "Elips"),
                ("➡️", DrawingTool.Arrow, "Ok"),
                ("🖍️", DrawingTool.Highlighter, "Fosforlu"),
                ("🧹", DrawingTool.Eraser, "Silgi")
            }

            Dim x = 20
            For Each t In tools
                Dim btn = CreateToolButton(t.Icon, t.Name, x, 20)
                AddHandler btn.Click, Sub(s, e)
                                          _currentTool = t.Tool
                                          UpdateCursor()
                                          HighlightToolButton(CType(s, Button))
                                      End Sub
                _toolPanel.Controls.Add(btn)
                x += 55
            Next

            ' Set default tool button
            _selectedToolButton = CType(_toolPanel.Controls(0), Button)
            HighlightToolButton(_selectedToolButton)

            ' Color picker
            _colorPanel = New Panel() With {
                .Location = New Point(x + 20, 20),
                .Size = New Size(200, 40),
                .BackColor = Color.Transparent
            }

            Dim colors = New Color() {Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.White, Color.Black, Color.Magenta, Color.Cyan}
            Dim cx = 0
            For Each c In colors
                Dim btnColor = CreateColorButton(c, cx)
                AddHandler btnColor.Click, Sub(s, e)
                                               _currentColor = c
                                               HighlightColorButton(CType(s, Button))
                                           End Sub
                _colorPanel.Controls.Add(btnColor)
                cx += 30
            Next

            _toolPanel.Controls.Add(_colorPanel)

            ' Size slider
            _sizeSlider = New TrackBar() With {
                .Location = New Point(x + 240, 15),
                .Size = New Size(100, 40),
                .Minimum = 1,
                .Maximum = 20,
                .Value = _currentSize,
                .TickFrequency = 2,
                .BackColor = Color.FromArgb(45, 45, 48)
            }
            AddHandler _sizeSlider.ValueChanged, Sub(s, e) _currentSize = _sizeSlider.Value
            _toolPanel.Controls.Add(_sizeSlider)

            ' Clear button
            _btnClear = CreateActionButton("Temizle (C)", x + 360, 20)
            AddHandler _btnClear.Click, Sub(s, e) ClearDrawing()
            _toolPanel.Controls.Add(_btnClear)

            ' Close button
            _btnClose = CreateActionButton("Kapat (Esc)", x + 470, 20)
            AddHandler _btnClose.Click, Sub(s, e) Close()
            _btnClose.BackColor = Color.FromArgb(220, 80, 60)
            _toolPanel.Controls.Add(_btnClose)

            ' Info label
            _lblInfo = New Label() With {
                .Text = "Sol Tık: Çiz | Sağ Tık: Sil | C: Temizle | Esc: Kapat",
                .Location = New Point(20, 65),
                .Size = New Size(600, 25),
                .ForeColor = Color.FromArgb(200, 200, 200),
                .BackColor = Color.Transparent,
                .Font = New Font("Segoe UI", 10)
            }
            _toolPanel.Controls.Add(_lblInfo)

            Controls.Add(_toolPanel)
            _toolPanel.BringToFront()
        End Sub

        Private Function CreateToolButton(icon As String, name As String, x As Integer, y As Integer) As Button
            Dim btn = New Button() With {
                .Text = icon,
                .Location = New Point(x, y),
                .Size = New Size(45, 45),
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI Emoji", 14),
                .ForeColor = Color.White,
                .BackColor = Color.FromArgb(80, 80, 80),
                .Cursor = Cursors.Hand,
                .Tag = name
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 100, 100)

            ' Tooltip
            Dim tt = New ToolTip()
            tt.SetToolTip(btn, name)

            Return btn
        End Function

        Private Function CreateColorButton(color As Color, x As Integer) As Button
            Dim btn = New Button() With {
                .Location = New Point(x, 5),
                .Size = New Size(25, 25),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = color,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.BorderColor = Color.White
            Return btn
        End Function

        Private Function CreateActionButton(text As String, x As Integer, y As Integer) As Button
            Dim btn = New Button() With {
                .Text = text,
                .Location = New Point(x, y),
                .Size = New Size(100, 40),
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.White,
                .BackColor = Color.FromArgb(0, 95, 184),
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 120, 212)
            Return btn
        End Function

        Private Sub HighlightToolButton(btn As Button)
            If _selectedToolButton IsNot Nothing Then
                _selectedToolButton.BackColor = Color.FromArgb(80, 80, 80)
            End If
            _selectedToolButton = btn
            _selectedToolButton.BackColor = Color.FromArgb(0, 120, 212)
        End Sub

        Private Sub HighlightColorButton(btn As Button)
            For Each c In _colorPanel.Controls
                CType(c, Button).FlatAppearance.BorderSize = 1
            Next
            btn.FlatAppearance.BorderSize = 3
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)

            If e.Button = MouseButtons.Left Then
                _isDrawing = True
                _lastPoint = e.Location

                Select Case _currentTool
                    Case DrawingTool.Pen, DrawingTool.Highlighter, DrawingTool.Eraser
                        _currentShape = New DrawingShape With {
                            .Tool = _currentTool,
                            .Color = If(_currentTool = DrawingTool.Eraser, Color.Transparent, _currentColor),
                            .Size = _currentSize,
                            .Points = New List(Of Point)() From {_lastPoint}
                        }

                    Case DrawingTool.Line, DrawingTool.Rectangle, DrawingTool.Ellipse, DrawingTool.Arrow
                        _currentShape = New DrawingShape With {
                            .Tool = _currentTool,
                            .Color = _currentColor,
                            .Size = _currentSize,
                            .StartPoint = _lastPoint,
                            .EndPoint = _lastPoint
                        }
                End Select
            End If
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)

            If _isDrawing AndAlso _currentShape IsNot Nothing Then
                Select Case _currentTool
                    Case DrawingTool.Pen
                        Using pen = New Pen(_currentColor, _currentSize)
                            pen.StartCap = Drawing2D.LineCap.Round
                            pen.EndCap = Drawing2D.LineCap.Round
                            _drawingGraphics.DrawLine(pen, _lastPoint, e.Location)
                        End Using
                        _currentShape.Points.Add(e.Location)
                        _lastPoint = e.Location
                        Invalidate()

                    Case DrawingTool.Highlighter
                        Using brush = New SolidBrush(Color.FromArgb(80, _currentColor))
                            _drawingGraphics.FillEllipse(brush, e.X - _currentSize, e.Y - _currentSize, _currentSize * 2, _currentSize * 2)
                        End Using
                        _currentShape.Points.Add(e.Location)
                        _lastPoint = e.Location
                        Invalidate()

                    Case DrawingTool.Eraser
                        Using path As New GraphicsPath()
                            path.AddEllipse(e.X - _currentSize * 2, e.Y - _currentSize * 2, _currentSize * 4, _currentSize * 4)
                            Using clipRegion = New Region(path)
                                _drawingGraphics.Clip = clipRegion
                                _drawingGraphics.Clear(Color.Transparent)
                                _drawingGraphics.ResetClip()
                            End Using
                        End Using
                        _currentShape.Points.Add(e.Location)
                        _lastPoint = e.Location
                        Invalidate()

                    Case DrawingTool.Line, DrawingTool.Rectangle, DrawingTool.Ellipse, DrawingTool.Arrow
                        _currentShape.EndPoint = e.Location
                        Invalidate()
                End Select
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)

            If _isDrawing AndAlso _currentShape IsNot Nothing Then
                _isDrawing = False

                Select Case _currentTool
                    Case DrawingTool.Line, DrawingTool.Rectangle, DrawingTool.Ellipse, DrawingTool.Arrow
                        DrawShape(_drawingGraphics, _currentShape)
                End Select

                _shapes.Add(_currentShape)
                RaiseEvent ShapeDrawn(Me, New ShapeDrawnEventArgs(_currentShape))
                _currentShape = Nothing
                Invalidate()
            End If
        End Sub

        Private Sub DrawShape(g As Graphics, shape As DrawingShape)
            Using pen = New Pen(shape.Color, shape.Size)
                pen.StartCap = Drawing2D.LineCap.Round
                pen.EndCap = Drawing2D.LineCap.Round

                Select Case shape.Tool
                    Case DrawingTool.Line
                        g.DrawLine(pen, shape.StartPoint, shape.EndPoint)

                    Case DrawingTool.Rectangle
                        Dim rect = GetRectangle(shape.StartPoint, shape.EndPoint)
                        g.DrawRectangle(pen, rect)

                    Case DrawingTool.Ellipse
                        Dim rect = GetRectangle(shape.StartPoint, shape.EndPoint)
                        g.DrawEllipse(pen, rect)

                    Case DrawingTool.Arrow
                        DrawArrow(g, pen, shape.StartPoint, shape.EndPoint)
                End Select
            End Using
        End Sub

        Private Function GetRectangle(p1 As Point, p2 As Point) As Rectangle
            Return Rectangle.FromLTRB(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Max(p1.X, p2.X),
                Math.Max(p1.Y, p2.Y))
        End Function

        Private Sub DrawArrow(g As Graphics, pen As Pen, start As Point, [end] As Point)
            g.DrawLine(pen, start, [end])

            Dim angle = Math.Atan2([end].Y - start.Y, [end].X - start.X)
            Dim arrowLength = 15
            Dim arrowAngle = 0.5

            Dim x1 = [end].X - arrowLength * Math.Cos(angle - arrowAngle)
            Dim y1 = [end].Y - arrowLength * Math.Sin(angle - arrowAngle)
            Dim x2 = [end].X - arrowLength * Math.Cos(angle + arrowAngle)
            Dim y2 = [end].Y - arrowLength * Math.Sin(angle + arrowAngle)

            g.DrawLine(pen, [end], New Point(CInt(x1), CInt(y1)))
            g.DrawLine(pen, [end], New Point(CInt(x2), CInt(y2)))
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)

            Dim g = e.Graphics

            If _drawingBitmap IsNot Nothing Then
                g.DrawImage(_drawingBitmap, Point.Empty)
            End If

            If _isDrawing AndAlso _currentShape IsNot Nothing Then
                DrawShape(g, _currentShape)
            End If
        End Sub

        Private Sub UpdateCursor()
            Select Case _currentTool
                Case DrawingTool.Pen, DrawingTool.Highlighter
                    Cursor = Cursors.Cross
                Case DrawingTool.Eraser
                    Dim bitmap = New Bitmap(32, 32)
                    Using g = Graphics.FromImage(bitmap)
                        g.Clear(Color.White)
                        Using brush = New SolidBrush(Color.Black)
                            g.FillEllipse(brush, 8, 8, 16, 16)
                        End Using
                    End Using
                    bitmap.MakeTransparent(Color.White)
                    Cursor = New Cursor(bitmap.GetHicon())
                    bitmap.Dispose()
                Case Else
                    Cursor = Cursors.Cross
            End Select
        End Sub

        Public Sub ClearDrawing()
            _shapes.Clear()
            _drawingGraphics.Clear(Color.Transparent)
            Invalidate()
        End Sub

        Public Function GetDrawingBitmap() As Bitmap
            Return CType(_drawingBitmap.Clone(), Bitmap)
        End Function

        Protected Overrides Sub OnFormClosing(e As System.Windows.Forms.FormClosingEventArgs)
            _drawingGraphics?.Dispose()
            _drawingBitmap?.Dispose()
            MyBase.OnFormClosing(e)
        End Sub

    End Class

    Public Class DrawingShape
        Public Property Tool As OverlayDrawingForm.DrawingTool
        Public Property Color As Color
        Public Property Size As Integer
        Public Property Points As List(Of Point)
        Public Property StartPoint As Point
        Public Property EndPoint As Point
        Public Property Text As String
    End Class

    Public Class ShapeDrawnEventArgs
        Inherits EventArgs

        Public ReadOnly Property Shape As DrawingShape

        Public Sub New(shape As DrawingShape)
            Me.Shape = shape
        End Sub
    End Class

End Namespace
