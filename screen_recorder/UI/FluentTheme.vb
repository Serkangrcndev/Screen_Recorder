Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

Namespace UI

    Public Class FluentTheme

        Public Shared ReadOnly PrimaryColor As Color = Color.FromArgb(0, 95, 184)
        Public Shared ReadOnly PrimaryDarkColor As Color = Color.FromArgb(0, 78, 152)
        Public Shared ReadOnly AccentColor As Color = Color.FromArgb(0, 120, 212)

        Public Shared ReadOnly DarkBackground As Color = Color.FromArgb(32, 32, 32)
        Public Shared ReadOnly DarkSurface As Color = Color.FromArgb(45, 45, 45)
        Public Shared ReadOnly DarkSurfaceElevated As Color = Color.FromArgb(50, 50, 50)

        Public Shared ReadOnly LightBackground As Color = Color.FromArgb(243, 243, 243)
        Public Shared ReadOnly LightSurface As Color = Color.FromArgb(255, 255, 255)
        Public Shared ReadOnly LightSurfaceElevated As Color = Color.FromArgb(250, 250, 250)

        Public Shared ReadOnly TextPrimaryDark As Color = Color.FromArgb(255, 255, 255)
        Public Shared ReadOnly TextSecondaryDark As Color = Color.FromArgb(200, 200, 200)
        Public Shared ReadOnly TextDisabledDark As Color = Color.FromArgb(120, 120, 120)

        Public Shared ReadOnly TextPrimaryLight As Color = Color.FromArgb(32, 32, 32)
        Public Shared ReadOnly TextSecondaryLight As Color = Color.FromArgb(100, 100, 100)
        Public Shared ReadOnly TextDisabledLight As Color = Color.FromArgb(150, 150, 150)

        Public Shared ReadOnly BorderDark As Color = Color.FromArgb(60, 60, 60)
        Public Shared ReadOnly BorderLight As Color = Color.FromArgb(220, 220, 220)

        Public Shared ReadOnly HoverDark As Color = Color.FromArgb(60, 60, 60)
        Public Shared ReadOnly PressedDark As Color = Color.FromArgb(80, 80, 80)
        Public Shared ReadOnly HoverLight As Color = Color.FromArgb(240, 240, 240)
        Public Shared ReadOnly PressedLight As Color = Color.FromArgb(230, 230, 230)

        Public Enum ThemeMode
            Dark
            Light
            System
        End Enum

        Public Shared Function GetBackground(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return DarkBackground
            End If
            Return LightBackground
        End Function

        Public Shared Function GetSurface(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return DarkSurface
            End If
            Return LightSurface
        End Function

        Public Shared Function GetSurfaceElevated(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return DarkSurfaceElevated
            End If
            Return LightSurfaceElevated
        End Function

        Public Shared Function GetTextPrimary(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return TextPrimaryDark
            End If
            Return TextPrimaryLight
        End Function

        Public Shared Function GetTextSecondary(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return TextSecondaryDark
            End If
            Return TextSecondaryLight
        End Function

        Public Shared Function GetBorder(mode As ThemeMode) As Color
            If mode = ThemeMode.Dark OrElse (mode = ThemeMode.System AndAlso IsDarkMode()) Then
                Return BorderDark
            End If
            Return BorderLight
        End Function

        Public Shared Function IsDarkMode() As Boolean
            Try
                Using key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")
                    If key IsNot Nothing Then
                        Dim value = key.GetValue("AppsUseLightTheme")
                        If value IsNot Nothing Then
                            Return CInt(value) = 0
                        End If
                    End If
                End Using
            Catch
            End Try
            Return False
        End Function

    End Class

    Public Class AcrylicEffect
        Inherits NativeWindow
        Implements IDisposable

        Private _form As Form
        Private _blurAmount As Integer = 20

        <DllImport("dwmapi.dll", PreserveSig:=False)>
        Private Shared Sub DwmSetWindowAttribute(hwnd As IntPtr, attr As Integer, ByRef attrValue As Integer, attrSize As Integer)
        End Sub

        <DllImport("dwmapi.dll", PreserveSig:=False)>
        Private Shared Sub DwmExtendFrameIntoClientArea(hwnd As IntPtr, ByRef margins As Margins)
        End Sub

        <DllImport("user32.dll")>
        Private Shared Function SetWindowCompositionAttribute(hwnd As IntPtr, ByRef data As WindowCompositionAttributeData) As Integer
        End Function

        Private Structure Margins
            Public Left As Integer
            Public Right As Integer
            Public Top As Integer
            Public Bottom As Integer
        End Structure

        Private Structure WindowCompositionAttributeData
            Public Attribute As Integer
            Public Data As IntPtr
            Public SizeOfData As Integer
        End Structure

        Private Const DWMWA_USE_IMMERSIVE_DARK_MODE As Integer = 20
        Private Const WCA_ACCENT_POLICY As Integer = 19

        Public Sub New(form As Form)
            _form = form
            AssignHandle(form.Handle)
            AddHandler form.HandleCreated, Sub(s, e) EnableMica()
            EnableMica()
        End Sub

        Public Sub EnableMica()
            Try
                Dim useDarkMode As Integer = If(FluentTheme.IsDarkMode(), 1, 0)
                DwmSetWindowAttribute(_form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, useDarkMode, Marshal.SizeOf(GetType(Integer)))

                Dim margins As New Margins() With {
                    .Left = -1,
                    .Right = -1,
                    .Top = -1,
                    .Bottom = -1
                }
                DwmExtendFrameIntoClientArea(_form.Handle, margins)

            Catch ex As Exception
                Debug.WriteLine($"Mica efekt hatası: {ex.Message}")
            End Try
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ReleaseHandle()
        End Sub

    End Class

    Public Class RoundedRectangle

        Public Shared Function Create(bounds As RectangleF, radius As Single) As GraphicsPath
            Dim path As New GraphicsPath()
            Dim diameter As Single = radius * 2
            Dim rect As New RectangleF(bounds.Location, New SizeF(diameter, diameter))

            path.AddArc(rect, 180, 90)
            rect.X = bounds.Right - diameter
            path.AddArc(rect, 270, 90)
            rect.Y = bounds.Bottom - diameter
            path.AddArc(rect, 0, 90)
            rect.X = bounds.Left
            path.AddArc(rect, 90, 90)
            path.CloseFigure()

            Return path
        End Function

        Public Shared Function Create(bounds As Rectangle, radius As Integer) As GraphicsPath
            Return Create(New RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius)
        End Function

    End Class

End Namespace
