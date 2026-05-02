Imports NAudio.CoreAudioApi
Imports NAudio.Wave
Imports System.IO
Imports System.Threading

Namespace Core

    Public Class AudioCapture
        Implements IDisposable

        Private _settings As Models.RecordingSettings
        Private _systemAudioWaveIn As WasapiLoopbackCapture
        Private _microphoneWaveIn As WaveInEvent
        Private _systemAudioWriter As WaveFileWriter
        Private _microphoneWriter As WaveFileWriter
        Private _systemAudioPath As String
        Private _microphonePath As String
        Private _isRunning As Boolean = False

        Public Property IsMicrophoneMuted As Boolean = False

        Public Event AudioDataAvailable(sender As Object, e As AudioDataEventArgs)
        Public Event RecordingError(sender As Object, e As Exception)

        Public Sub New(settings As Models.RecordingSettings)
            _settings = settings
        End Sub

        Public Sub Start()
            If _isRunning Then Return

            Dim tempFolder = Path.Combine(Path.GetTempPath(), "ScreenRecorder")
            Directory.CreateDirectory(tempFolder)

            Try
                If _settings.RecordSystemAudio Then
                    StartSystemAudioCapture(tempFolder)
                End If

                If _settings.RecordMicrophone AndAlso Not String.IsNullOrEmpty(_settings.MicrophoneDevice) Then
                    StartMicrophoneCapture(tempFolder)
                End If

                _isRunning = True

            Catch ex As Exception
                RaiseEvent RecordingError(Me, ex)
                Cleanup()
            End Try
        End Sub

        Private Sub StartSystemAudioCapture(tempFolder As String)
            _systemAudioPath = Path.Combine(tempFolder, $"system_audio_{Guid.NewGuid():N}.wav")

            _systemAudioWaveIn = New WasapiLoopbackCapture()
            _systemAudioWriter = New WaveFileWriter(_systemAudioPath, _systemAudioWaveIn.WaveFormat)

            AddHandler _systemAudioWaveIn.DataAvailable, Sub(s, e)
                                                             If e.BytesRecorded > 0 AndAlso _systemAudioWriter IsNot Nothing Then
                                                                 _systemAudioWriter.Write(e.Buffer, 0, e.BytesRecorded)
                                                                 RaiseEvent AudioDataAvailable(Me, New AudioDataEventArgs("System", e.BytesRecorded))
                                                             End If
                                                         End Sub

            AddHandler _systemAudioWaveIn.RecordingStopped, Sub(s, e)
                                                                _systemAudioWriter?.Flush()
                                                            End Sub

            _systemAudioWaveIn.StartRecording()
        End Sub

        Private Sub StartMicrophoneCapture(tempFolder As String)
            _microphonePath = Path.Combine(tempFolder, $"microphone_{Guid.NewGuid():N}.wav")

            Dim deviceNumber = GetMicrophoneDeviceNumber(_settings.MicrophoneDevice)
            _microphoneWaveIn = New WaveInEvent()
            _microphoneWaveIn.DeviceNumber = deviceNumber
            _microphoneWaveIn.WaveFormat = New WaveFormat(44100, 16, 2)

            _microphoneWriter = New WaveFileWriter(_microphonePath, _microphoneWaveIn.WaveFormat)

            AddHandler _microphoneWaveIn.DataAvailable, Sub(s, e)
                                                           If e.BytesRecorded > 0 AndAlso _microphoneWriter IsNot Nothing Then
                                                               If IsMicrophoneMuted Then
                                                                   ' Write silence to maintain A/V sync
                                                                   Dim silence(e.BytesRecorded - 1) As Byte
                                                                   _microphoneWriter.Write(silence, 0, e.BytesRecorded)
                                                               Else
                                                                   _microphoneWriter.Write(e.Buffer, 0, e.BytesRecorded)
                                                               End If
                                                               RaiseEvent AudioDataAvailable(Me, New AudioDataEventArgs("Microphone", e.BytesRecorded))
                                                           End If
                                                       End Sub

            AddHandler _microphoneWaveIn.RecordingStopped, Sub(s, e)
                                                               _microphoneWriter?.Flush()
                                                           End Sub

            _microphoneWaveIn.StartRecording()
        End Sub

        Private Function GetMicrophoneDeviceNumber(deviceName As String) As Integer
            For i As Integer = 0 To WaveIn.DeviceCount - 1
                Dim capabilities = WaveIn.GetCapabilities(i)
                If capabilities.ProductName.Contains(deviceName) Then
                    Return i
                End If
            Next
            Return 0
        End Function

        Public Sub [Stop]()
            If Not _isRunning Then Return

            _isRunning = False

            Try
                _systemAudioWaveIn?.StopRecording()
                _microphoneWaveIn?.StopRecording()

                Thread.Sleep(100)

                Cleanup()

            Catch ex As Exception
                RaiseEvent RecordingError(Me, ex)
            End Try
        End Sub

        Private Sub Cleanup()
            Try
                _systemAudioWaveIn?.Dispose()
                _systemAudioWaveIn = Nothing

                _microphoneWaveIn?.Dispose()
                _microphoneWaveIn = Nothing

                _systemAudioWriter?.Flush()
                _systemAudioWriter?.Dispose()
                _systemAudioWriter = Nothing

                _microphoneWriter?.Flush()
                _microphoneWriter?.Dispose()
                _microphoneWriter = Nothing
            Catch
            End Try
        End Sub

        Public Function GetSystemAudioPath() As String
            Return _systemAudioPath
        End Function

        Public Function GetMicrophonePath() As String
            Return _microphonePath
        End Function

        Public Shared Function GetAudioDevices() As List(Of String)
            Dim devices As New List(Of String)()

            For i As Integer = 0 To WaveIn.DeviceCount - 1
                Dim capabilities = WaveIn.GetCapabilities(i)
                devices.Add(capabilities.ProductName)
            Next

            Return devices
        End Function

        Public Shared Function GetSystemAudioDevices() As List(Of String)
            Dim devices As New List(Of String)()

            Dim enumerator = New MMDeviceEnumerator()
            Dim collection = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)

            For Each device In collection
                devices.Add(device.FriendlyName)
            Next

            Return devices
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
        End Sub

    End Class

    Public Class AudioDataEventArgs
        Inherits EventArgs

        Public ReadOnly Property Source As String
        Public ReadOnly Property BytesRecorded As Integer

        Public Sub New(source As String, bytesRecorded As Integer)
            Me.Source = source
            Me.BytesRecorded = bytesRecorded
        End Sub
    End Class

End Namespace
