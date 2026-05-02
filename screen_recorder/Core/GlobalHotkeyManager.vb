Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Core

    Public Class GlobalHotkeyManager
        Implements IDisposable

        Private _hwnd As IntPtr
        Private _registeredHotkeys As New Dictionary(Of Integer, HotkeyDefinition)()
        Private _callbackDelegate As NativeMethods.HotkeyCallback
        Private _messageWindow As MessageWindow

        Public Event HotkeyPressed(sender As Object, e As HotkeyPressedEventArgs)

        Private Structure HotkeyDefinition
            Public Id As Integer
            Public Key As Keys
            Public Modifiers As KeyModifiers
            Public Description As String
        End Structure

        <Flags()>
        Public Enum KeyModifiers
            None = 0
            Alt = 1
            Control = 2
            Shift = 4
            Win = 8
        End Enum

        Public Sub New()
            _callbackDelegate = AddressOf HotkeyCallback
            _messageWindow = New MessageWindow(_callbackDelegate)
            _hwnd = _messageWindow.Handle
        End Sub

        Public Function RegisterHotkey(id As Integer, key As Keys, modifiers As KeyModifiers, description As String) As Boolean
            If _registeredHotkeys.ContainsKey(id) Then
                UnregisterHotkey(id)
            End If

            Dim result = NativeMethods.RegisterHotKey(_hwnd, id, modifiers, key)

            If result Then
                _registeredHotkeys(id) = New HotkeyDefinition With {
                    .Id = id,
                    .Key = key,
                    .Modifiers = modifiers,
                    .Description = description
                }
                Return True
            End If

            Return False
        End Function

        Public Function UnregisterHotkey(id As Integer) As Boolean
            If _registeredHotkeys.ContainsKey(id) Then
                Dim result = NativeMethods.UnregisterHotKey(_hwnd, id)
                _registeredHotkeys.Remove(id)
                Return result
            End If
            Return False
        End Function

        Public Sub UnregisterAllHotkeys()
            For Each id In _registeredHotkeys.Keys.ToList()
                UnregisterHotkey(id)
            Next
        End Sub

        Private Function HotkeyCallback(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
            If msg = NativeMethods.WM_HOTKEY Then
                Dim id = wParam.ToInt32()
                Dim key = CType((lParam.ToInt32() And &HFFFF), Keys)
                Dim modifiers = CType((lParam.ToInt32() >> 16), KeyModifiers)

                If _registeredHotkeys.ContainsKey(id) Then
                    Dim hotkey = _registeredHotkeys(id)
                    RaiseEvent HotkeyPressed(Me, New HotkeyPressedEventArgs(id, hotkey.Key, hotkey.Modifiers, hotkey.Description))
                    handled = True
                End If
            End If

            Return IntPtr.Zero
        End Function

        Public Function GetRegisteredHotkeys() As IEnumerable(Of String)
            Return _registeredHotkeys.Values.Select(Function(h) $"{h.Description}: {GetModifierString(h.Modifiers)}{h.Key}").ToList()
        End Function

        Private Function GetModifierString(modifiers As KeyModifiers) As String
            Dim parts As New List(Of String)()
            If modifiers And KeyModifiers.Control Then parts.Add("Ctrl+")
            If modifiers And KeyModifiers.Alt Then parts.Add("Alt+")
            If modifiers And KeyModifiers.Shift Then parts.Add("Shift+")
            If modifiers And KeyModifiers.Win Then parts.Add("Win+")
            Return String.Join("", parts)
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            UnregisterAllHotkeys()
            _messageWindow?.Dispose()
        End Sub

    End Class

    Public Class HotkeyPressedEventArgs
        Inherits EventArgs

        Public ReadOnly Property Id As Integer
        Public ReadOnly Property Key As Keys
        Public ReadOnly Property Modifiers As GlobalHotkeyManager.KeyModifiers
        Public ReadOnly Property Description As String

        Public Sub New(id As Integer, key As Keys, modifiers As GlobalHotkeyManager.KeyModifiers, description As String)
            Me.Id = id
            Me.Key = key
            Me.Modifiers = modifiers
            Me.Description = description
        End Sub
    End Class

    Public Class MessageWindow
        Inherits NativeWindow
        Implements IDisposable

        Private _callback As NativeMethods.HotkeyCallback

        Public Sub New(callback As NativeMethods.HotkeyCallback)
            _callback = callback
            CreateHandle(New CreateParams() With {
                .ExStyle = NativeMethods.WS_EX_TOOLWINDOW,
                .Style = NativeMethods.WS_OVERLAPPED
            })
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            Dim handled = False
            _callback?.Invoke(m.HWnd, m.Msg, m.WParam, m.LParam, handled)

            If Not handled Then
                MyBase.WndProc(m)
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            DestroyHandle()
        End Sub

    End Class

    Public Module NativeMethods

        Public Const WM_HOTKEY As Integer = &H312
        Public Const WS_EX_TOOLWINDOW As Integer = &H80
        Public Const WS_OVERLAPPED As Integer = &H0

        Public Delegate Function HotkeyCallback(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr

        <DllImport("user32.dll", SetLastError:=True)>
        Public Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As GlobalHotkeyManager.KeyModifiers, vk As Keys) As Boolean
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Public Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
        End Function

    End Module

End Namespace
