namespace SchreckLib.Networking
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Net.Sockets;
    public class Server : Common
    {
        public event EventHandler<ListenEventArgs> BeforeListen;
        public event EventHandler<ListenEventArgs> AfterListen;
        public event EventHandler<AcceptEventArgs> BeforeAccept;
        public event EventHandler<AcceptEventArgs> AfterAccept;

        protected Thread listenThread;
        protected ManualResetEvent manualResetEvent;
        protected Hashtable sockets;
        protected int backlog;
        ~Server()
        {
            isClosing = true;
            if (manualResetEvent != null)
                manualResetEvent.Set();
            if (listenThread != null)
                listenThread.Join();
            if (sockets != null && sockets.Count > 0)
            {
                foreach(Socket socket in sockets) {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                sockets.Clear();
            }
            sockets = null;
            listenThread = null;
            manualResetEvent = null;
        }
    }
}
/*

#Region "Methods"
#Region "Private Methods"
        ''' <summary>
        ''' Starts the server's socket in listening mode. This is called from Me.Listen.
        ''' </summary>
        ''' <remarks>
        ''' JMS - 1-6-07 - Created
        ''' </remarks>
        Private Sub __DoListen()
            If Thread.CurrentThread.Name IsNot "Listening Thread" Then
                Thread.CurrentThread.Name = "Listening Thread"
            End If

            Try

                Me.__oManualResetEvent = New ManualResetEvent(True)

                Me.__oSockets = New Hashtable

                If Me.__oManualResetEvent Is Nothing Then
                    Me._OnError("Could not create a ManualResetEvent")
                    Exit Sub
                End If

                'Tell this socket where to listen from
                Me._oSocket.Bind(Me._oEndPoint)

                RaiseEvent BeforeListen(Me, New ListenEventArgs(Me._oEndPoint, Me.__iBackLog))

                Me._OnListen()

                'Let the handler know that server is listening
                RaiseEvent AfterListen(Me, New ListenEventArgs(Me._oEndPoint, Me.__iBackLog))

                'Now run an I-Loop to continuously accept connections
                Do
                    If Me._lClosing Then Exit Do

                    Me.__oManualResetEvent.Reset()

                    'Start the accept process
                    Me._oSocket.BeginAccept(New AsyncCallback(AddressOf __AcceptCallback), Nothing)

                    'Pause this thread
                    'If this fails, It is because the server is shutting down
                    Me.__oManualResetEvent.WaitOne()

                Loop

            Catch ex As Exception

                Me._OnException(ex)

            End Try
        End Sub
        ''' <summary>
        ''' The Asynchronous Function used for accepting a connection
        ''' </summary>
        ''' <param name="toAsyncResult">iAsyncResult passed from me.oSocket.BeginAccept</param>
        ''' <remarks>
        ''' JMS - 1-6-07 - Created
        ''' JMS - 1-7-07 - Commented
        ''' </remarks>
        Private Sub __AcceptCallback(ByVal toAsyncResult As IAsyncResult)
            If Thread.CurrentThread.Name IsNot "Accepting Thread" Then
                Thread.CurrentThread.Name = "Accepting Thread"
            End If

            Dim laBuffer(1024) As Byte 'The buffer from the socket 'JMS set this in settings
            Dim loSocket As Socket
            Try

                If Me._lClosing Then
                    'Thread.CurrentThread.Abort()
                    Exit Try
                End If

                'Accept the user
                loSocket = Me._oSocket.EndAccept(toAsyncResult)

                'Let the Listening I-Loop continue
                Me.__oManualResetEvent.Set()

                Dim loAcceptEventArgs As AcceptEventArgs = New AcceptEventArgs(loSocket.RemoteEndPoint)

                SyncLock Me.__oSockets.SyncRoot
                    Me.__oSockets.Add(loAcceptEventArgs.GetIpAddress & ":" & loAcceptEventArgs.GetPort, loSocket)
                End SyncLock

                RaiseEvent BeforeAccept(Me, loAcceptEventArgs)
                'Anything that needs to be done on connection can be done below
                '--------------------DO NOT REMOVE THIS LINE-------------------

                Me._OnConnectionAccepted(loSocket)

                '--------------------DO NOT REMOVE THIS LINE-------------------
                'Anything that needs to be done on connection can be done above

                RaiseEvent AfterAccept(Me, loAcceptEventArgs)

                'Create our state object. The receiveCallback needs the buffer and the socket info.
                Dim loStateObject As StateObject = Me._CreateStateObject

                With loStateObject
                    .aBuffer = laBuffer
                    .iBufferLength = laBuffer.Length
                    .oSocket = loSocket
                End With

                'Begin Receiving any messages
                loStateObject.oSocket.BeginReceive(laBuffer, 0, laBuffer.Length, SocketFlags.None, _
                        AddressOf Me._ReceiveCallback, loStateObject)

            Catch ex As Exception

                Me._OnException(ex)

            End Try

        End Sub
#End Region
#Region "Protected Methods"
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub _OnListen()

            'Start Listening
            Me._oSocket.Listen(Me.__iBackLog)

        End Sub
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="oSocket"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub _OnConnectionAccepted(ByVal oSocket As Socket)

        End Sub
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="toSocket"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub _OnDisconnect(ByRef toSocket As Socket)
            Dim loSocketEventArg As New DisconnectEventArgs(toSocket.RemoteEndPoint)
            MyBase._OnDisconnect(toSocket)
            Dim loSocket As Socket = CType(Me.__oSockets.Item(loSocketEventArg.GetIpAddress & ":" & loSocketEventArg.GetPort), Socket)
            Me.__oSockets.Remove(loSocketEventArg.GetIpAddress & ":" & loSocketEventArg.GetPort)
        End Sub
#End Region
#Region "Public Methods"
        ''' <summary>
        ''' This Sub will allow the server to listen on whichever port was specified
        ''' during construction
        ''' </summary>
        ''' <param name="tiBackLog">The maximum length of pending connections queue</param>
        ''' <remarks>
        ''' Created 3-19-2008
        ''' </remarks>
        Public Sub Listen(Optional ByVal tiBackLog As Int32 = 0)

            If tiBackLog <> 0 Then Me.__iBackLog = tiBackLog

            Me._oException = Nothing

            Me.__oListenThread = New Thread(AddressOf Me.__DoListen)

            Try

                Me.__oListenThread.Start()

            Catch ex As Exception

                Me._OnException(ex)

            End Try

        End Sub
        ''' <summary>
        ''' This function can be used to send information on a socket
        ''' </summary>
        ''' <param name="taMessage">The message in byte-array format
        ''' to be sent through the socket</param>
        ''' <remarks>
        ''' JMS - 2-1-07 - Created
        ''' </remarks>
        Public Overloads Sub Send(ByVal taMessage() As Byte)
            Dim loSocket As Socket

            If Me.__oSockets IsNot Nothing And Me.__oSockets.Count > 0 Then

                RaiseEvent BeforeSend(Me, New BeforeSendEventArgs(taMessage, Me.__oSockets.Count))

                For Each loDictionaryEntry As DictionaryEntry In Me.__oSockets
                    loSocket = CType(loDictionaryEntry.Value, Socket)
                    Try
                        loSocket.Send(taMessage)
                    Catch oException As Exception
                        Debugger.Break()
                        Me._OnException(oException)
                    End Try
                Next

                RaiseEvent AfterSend(Me, New AfterSendEventArgs(taMessage, Me.__oSockets.Keys))
            End If
        End Sub
        ''' <summary>
        ''' This function can be used to send information on a socket
        ''' </summary>
        ''' <param name="tcMessage">The message in string format
        ''' to be sent through the socket</param>
        ''' <remarks>
        ''' JMS - 1-1-07 - Created (Happy New Year)
        ''' </remarks>
        Public Overloads Sub Send(ByVal tcMessage As String)

            Dim loAsciiEncoder As New Text.ASCIIEncoding()

            Me.Send(loAsciiEncoder.GetBytes(tcMessage))

            loAsciiEncoder = Nothing

        End Sub
#End Region
#End Region
#Region "EventArgs Classes"
        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Public Class ListenEventArgs
            Inherits EndpointEventArgs
            Private __iBackLog As Integer

            Public Sub New(ByRef toEndPoint As IPEndPoint, ByVal tiBackLog As Integer)
                MyBase.New(toEndPoint)
                Me.__iBackLog = tiBackLog
            End Sub
            Public ReadOnly Property GetBackLog() As Integer
                Get
                    Return Me.__iBackLog
                End Get
            End Property
        End Class
        Public Class AcceptEventArgs
            Inherits EndpointEventArgs
            Public Sub New(ByRef toRemoteEndPoint As IPEndPoint)
                MyBase.New(toRemoteEndPoint)
            End Sub
            Public Sub New(ByRef toRemoteEndPoint As EndPoint)
                MyBase.New(toRemoteEndPoint)
            End Sub
        End Class
        Public Class BeforeSendEventArgs
            Inherits SendReceiveEventArgs
            Private __iRecipients As Integer
            Public Sub New(ByRef taMessage As Byte(), ByVal tiRecipients As Integer)
                MyBase.New(Nothing, taMessage)
                Me.__iRecipients = tiRecipients
            End Sub
            Public Overloads ReadOnly Property GetIpAddress() As String
                Get
                    Return "Unknown"
                End Get
            End Property
            Public Overloads ReadOnly Property GetPort() As String
                Get
                    Return "0"
                End Get
            End Property
            Public ReadOnly Property GetRecipientCount() As Integer
                Get
                    Return Me.__iRecipients
                End Get
            End Property
        End Class
        Public Class AfterSendEventArgs
            Inherits BeforeSendEventArgs
            Private __aRecipients As ArrayList
            Public Sub New(ByRef taMessage As Byte(), ByRef toRecipients As ICollection)
                MyBase.New(taMessage, toRecipients.Count)
                Me.__aRecipients = New ArrayList
                For Each lsIp As String In toRecipients
                    Me.__aRecipients.Add(lsIp)
                Next
            End Sub
            Public ReadOnly Property GetRecipients() As ArrayList
                Get
                    Return Me.__aRecipients
                End Get
            End Property
        End Class
#End Region
    End Class
End Namespace

*/