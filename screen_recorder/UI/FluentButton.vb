Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    Public Class FluentButton
        Inherits Button

        Private _isHovered As Boolean = False
        Private _isPressed As Boolean = False
        Private _cornerRadius As Integer = 4
        Private _themeMode As FluentTheme.ThemeMode = FluentTheme.ThemeMode.System

        Public Property CornerRadius As Integer
            Get
                Return _cornerRadius
            End Get
            Set(value As Integer)
                _cornerRadius = value
                Invalidate()
            End Set
        End Property

        Public Property ThemeMode As FluentTheme.ThemeMode
            Get
                Return _themeMode
            End Get
            Set(value As FluentTheme.ThemeMode)
                _themeMode = value
                Invalidate()
            End Set
        End Property

        Public Sub New()
            SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, True)
            FlatStyle = FlatStyle.Flat
            FlatAppearance.BorderSize = 0
            Font = New Font("Segoe UI Variable", 9.0F, FontStyle.Regular)
            Cursor = Cursors.Hand
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.Half

            Dim rect = ClientRectangle

            Dim bgColor = GetBackgroundColor()
            Dim textColor = GetTextColor()

            Using path = RoundedRectangle.Create(rect, _cornerRadius)
                Using brush = New SolidBrush(bgColor)
                    g.FillPath(brush, path)
                End Using

                If _isHovered Then
                    Using hoverBrush = New SolidBrush(GetHoverColor())
                        g.FillPath(hoverBrush, path)
                    End Using
                End If

                If _isPressed Then
                    Using pressedBrush = New SolidBrush(GetPressedColor())
                        g.FillPath(pressedBrush, path)
                    End Using
                End If

                Using pen = New Pen(FluentTheme.GetBorder(_themeMode), 1)
                    g.DrawPath(pen, path)
                End Using
            End Using

            TextRenderer.DrawText(g, Text, Font, rect, textColor, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            _isHovered = True
            Invalidate()
            MyBase.OnMouseEnter(e)
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            _isHovered = False
            _isPressed = False
            Invalidate()
            MyBase.OnMouseLeave(e)
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            _isPressed = True
            Invalidate()
            MyBase.OnMouseDown(e)
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            _isPressed = False
            Invalidate()
            MyBase.OnMouseUp(e)
        End Sub

        Protected Overridable Function GetBackgroundColor() As Color
            If Enabled Then
                If Tag IsNot Nothing AndAlso Tag.ToString() = "Primary" Then
                    Return FluentTheme.PrimaryColor
                End If
                Return FluentTheme.GetSurface(_themeMode)
            End If
            Return FluentTheme.GetSurface(_themeMode)
        End Function

        Protected Overridable Function GetTextColor() As Color
            If Not Enabled Then
                If FluentTheme.IsDarkMode() Then
                    Return FluentTheme.TextDisabledDark
                Else
                    Return FluentTheme.TextDisabledLight
                End If
            End If

            If Tag IsNot Nothing AndAlso Tag.ToString() = "Primary" Then
                Return Color.White
            End If

            Return FluentTheme.GetTextPrimary(_themeMode)
        End Function

        Protected Overridable Function GetHoverColor() As Color
            If Tag IsNot Nothing AndAlso Tag.ToString() = "Primary" Then
                Return FluentTheme.PrimaryDarkColor
            End If

            If FluentTheme.IsDarkMode() Then
                Return FluentTheme.HoverDark
            Else
                Return FluentTheme.HoverLight
            End If
        End Function

        Protected Overridable Function GetPressedColor() As Color
            If Tag IsNot Nothing AndAlso Tag.ToString() = "Primary" Then
                Return Color.FromArgb(0, 60, 120)
            End If

            If FluentTheme.IsDarkMode() Then
                Return FluentTheme.PressedDark
            Else
                Return FluentTheme.PressedLight
            End If
        End Function

    End Class

    Public Class FluentToggleButton
        Inherits FluentButton

        Private _isChecked As Boolean = False

        Public Property IsChecked As Boolean
            Get
                Return _isChecked
            End Get
            Set(value As Boolean)
                _isChecked = value
                Invalidate()
            End Set
        End Property

        Protected Overrides Sub OnClick(e As EventArgs)
            _isChecked = Not _isChecked
            MyBase.OnClick(e)
        End Sub

        Protected Overrides Function GetBackgroundColor() As Color
            If _isChecked Then
                Return FluentTheme.PrimaryColor
            End If
            Return MyBase.GetBackgroundColor()
        End Function

        Protected Overrides Function GetTextColor() As Color
            If _isChecked Then
                Return Color.White
            End If
            Return MyBase.GetTextColor()
        End Function

        Protected Overrides Function GetHoverColor() As Color
            If _isChecked Then
                Return FluentTheme.PrimaryDarkColor
            End If
            Return MyBase.GetHoverColor()
        End Function

    End Class

End Namespace
