Imports System
Imports System.Diagnostics
Imports System.Runtime
Imports System.Runtime.InteropServices
Imports System.Threading

Namespace Core

    Public Class PerformanceManager
        Implements IDisposable

        Private _process As Process
        Private _memoryMonitorTimer As Timer
        Private _gcTimer As Timer
        Private _lastFrameTime As Long
        private _frameCount As Integer = 0
        Private _fps As Integer = 0
        Private _isHighPerformance As Boolean = False

        Public ReadOnly Property CurrentFPS As Integer
            Get
                Return _fps
            End Get
        End Property

        Public ReadOnly Property MemoryUsageMB As Double
            Get
                Try
                    _process.Refresh()
                    Return _process.WorkingSet64 / (1024 * 1024)
                Catch
                    Return 0
                End Try
            End Get
        End Property

        Public Sub New()
            _process = Process.GetCurrentProcess()
            InitializePerformanceOptimization()
            StartMonitoring()
        End Sub

        Private Sub InitializePerformanceOptimization()
            Try
                _process.PriorityClass = ProcessPriorityClass.High
                _process.ProcessorAffinity = New IntPtr(&HFFFF)

                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency

                If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                    SetProcessWorkingSetSize(_process.Handle, -1, -1)
                End If

            Catch ex As Exception
                Debug.WriteLine($"Performans optimizasyonu hatası: {ex.Message}")
            End Try
        End Sub

        Public Sub SetHighPerformanceMode(enable As Boolean)
            _isHighPerformance = enable

            Try
                If enable Then
                    _process.PriorityClass = ProcessPriorityClass.RealTime
                    GCSettings.LatencyMode = GCLatencyMode.LowLatency
                Else
                    _process.PriorityClass = ProcessPriorityClass.High
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency
                End If
            Catch ex As Exception
                Debug.WriteLine($"Performans modu değiştirme hatası: {ex.Message}")
            End Try
        End Sub

        Private Sub StartMonitoring()
            _memoryMonitorTimer = New Timer(AddressOf OnMemoryMonitorTick, Nothing, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))
            _gcTimer = New Timer(AddressOf OnGCTimerTick, Nothing, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))

            Dim fpsThread As New Thread(AddressOf CalculateFPS)
            fpsThread.IsBackground = True
            fpsThread.Start()
        End Sub

        Private Sub OnMemoryMonitorTick(state As Object)
            Try
                Dim memoryMB = MemoryUsageMB

                If memoryMB > 512 Then
                    Dim startInfo As New ProcessStartInfo("taskkill", "/im YourProcessName.exe /f")
                    startInfo.CreateNoWindow = True
                    startInfo.UseShellExecute = False
                    Dim proc = Process.Start(startInfo)
                    proc.WaitForExit(5000)

                    If Not proc.HasExited Then
                        proc.Kill()
                    End If

                    proc.Dispose()
                End If

                If memoryMB > 1024 Then
                    TrimWorkingSet()
                End If

            Catch ex As Exception
                Debug.WriteLine($"Bellek izleme hatası: {ex.Message}")
            End Try
        End Sub

        Private Sub OnGCTimerTick(state As Object)
            ForceGarbageCollection()
        End Sub

        Private Sub CalculateFPS()
            While True
                Thread.Sleep(1000)
                _fps = _frameCount
                _frameCount = 0
            End While
        End Sub

        Public Sub NotifyFrameProcessed()
            _frameCount += 1
        End Sub

        Public Shared Sub ForceGarbageCollection()
            Try
                GC.Collect(2, GCCollectionMode.Optimized, False)
                GC.WaitForPendingFinalizers()
                GC.Collect(2, GCCollectionMode.Optimized, False)
            Catch
            End Try
        End Sub

        Public Sub TrimWorkingSet()
            Try
                GC.Collect()
                GC.WaitForPendingFinalizers()

                If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
                    SetProcessWorkingSetSize(_process.Handle, -1, -1)
                    EmptyWorkingSet(_process.Handle)
                End If

            Catch ex As Exception
                Debug.WriteLine($"Working set temizleme hatası: {ex.Message}")
            End Try
        End Sub

        Public Sub PreallocateBuffers(frameWidth As Integer, frameHeight As Integer, bufferCount As Integer)
            Try
                For i As Integer = 0 To bufferCount - 1
                    Dim bufferSize = frameWidth * frameHeight * 4
                    Dim buffer(bufferSize - 1) As Byte
                    buffer(0) = 0
                Next

                ForceGarbageCollection()

            Catch ex As Exception
                Debug.WriteLine($"Buffer ön tahsis hatası: {ex.Message}")
            End Try
        End Sub

        Public Shared Sub OptimizeForScreenRecording()
            Try
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency

                Dim currentProcess = Process.GetCurrentProcess()
                currentProcess.PriorityClass = ProcessPriorityClass.High

                Thread.CurrentThread.Priority = ThreadPriority.Highest

            Catch ex As Exception
                Debug.WriteLine($"Optimizasyon hatası: {ex.Message}")
            End Try
        End Sub

        Public Shared Sub RestoreNormalMode()
            Try
                GCSettings.LatencyMode = GCLatencyMode.Interactive

                Dim currentProcess = Process.GetCurrentProcess()
                currentProcess.PriorityClass = ProcessPriorityClass.Normal

                Thread.CurrentThread.Priority = ThreadPriority.Normal

            Catch ex As Exception
                Debug.WriteLine($"Normal moda dönme hatası: {ex.Message}")
            End Try
        End Sub

        <DllImport("kernel32.dll")>
        Private Shared Function SetProcessWorkingSetSize(hProcess As IntPtr, dwMinimumWorkingSetSize As IntPtr, dwMaximumWorkingSetSize As IntPtr) As Boolean
        End Function

        <DllImport("psapi.dll")>
        Private Shared Function EmptyWorkingSet(hProcess As IntPtr) As Boolean
        End Function

        <DllImport("kernel32.dll")>
        Private Shared Function GetProcessMemoryInfo(hProcess As IntPtr, ByRef memCounter As PROCESS_MEMORY_COUNTERS_EX, cb As Integer) As Boolean
        End Function

        <StructLayout(LayoutKind.Sequential)>
        Private Structure PROCESS_MEMORY_COUNTERS_EX
            Public cb As Integer
            Public PageFaultCount As Integer
            Public PeakWorkingSetSize As IntPtr
            Public WorkingSetSize As IntPtr
            Public QuotaPeakPagedPoolUsage As IntPtr
            Public QuotaPagedPoolUsage As IntPtr
            Public QuotaPeakNonPagedPoolUsage As IntPtr
            Public QuotaNonPagedPoolUsage As IntPtr
            Public PagefileUsage As IntPtr
            Public PeakPagefileUsage As IntPtr
            Public PrivateUsage As IntPtr
        End Structure

        Public Sub Dispose() Implements IDisposable.Dispose
            _memoryMonitorTimer?.Dispose()
            _gcTimer?.Dispose()
        End Sub

    End Class

    Public Class FrameBufferPool
        Private _buffers As New Collections.Concurrent.ConcurrentBag(Of Byte())()
        Private _bufferSize As Integer
        Private _maxPoolSize As Integer
        Private _currentSize As Integer = 0
        Private _lock As New Object()

        Public Sub New(bufferSize As Integer, maxPoolSize As Integer)
            _bufferSize = bufferSize
            _maxPoolSize = maxPoolSize
        End Sub

        Public Function Rent() As Byte()
            Dim buffer As Byte() = Nothing

            If _buffers.TryTake(buffer) AndAlso buffer IsNot Nothing Then
                Interlocked.Decrement(_currentSize)
                Return buffer
            End If

            Return New Byte(_bufferSize - 1) {}
        End Function

        Public Sub ReturnBuffer(buffer As Byte())
            If buffer Is Nothing OrElse buffer.Length <> _bufferSize Then
                Return
            End If

            SyncLock _lock
                If _currentSize < _maxPoolSize Then
                    Array.Clear(buffer, 0, buffer.Length)
                    _buffers.Add(buffer)
                    Interlocked.Increment(_currentSize)
                End If
            End SyncLock
        End Sub

        Public Sub Clear()
            While _buffers.TryTake(Nothing)
                Interlocked.Decrement(_currentSize)
            End While
        End Sub

    End Class

End Namespace
