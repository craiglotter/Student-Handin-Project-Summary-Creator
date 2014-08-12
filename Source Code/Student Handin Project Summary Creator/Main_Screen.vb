Imports System
Imports System.IO
Imports System.Collections
Imports System.ComponentModel
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms


Public Class Main_Screen

    Private application_busy_exiting As Boolean = False
    Private busyworking As Boolean = False

    Private lastinputline As String = ""
    Private lastinputlineFull As String = ""
    Private inputlines As Long = 0
    Private statusmessage As String = ""
    Private statusresults As String = ""
    Private highestPercentageReached As Integer = 0
    Private inputlinesprecount As Long = 0
    Private datelaunched As Date = Now()
    Private pretestdone As Boolean = False
    
    Private primary_PercentComplete As Integer = 0

    Private secondary_inputlinesprecount As Long = 0
    Private secondary_inputlines As Long = 0
    Private secondary_inputlinesTotal As Long = 0
    Private secondary_lastinputline As String = ""
    Private secondary_highestPercentageReached As Integer = 0
    Private secondary_PercentComplete As Integer = 0
    Private secondary_lastinputlineFull As String = ""

    Private TimesExecuted As Integer = 0

    Private Sub Error_Handler(ByVal ex As Exception, Optional ByVal identifier_msg As String = "")
        Try
            If ex.Message.IndexOf("Thread was being aborted") < 0 Then
                Dim Display_Message1 As New Display_Message()
                If FullErrors_Checkbox.Checked = True Then
                    Display_Message1.Message_Textbox.Text = "The Application encountered the following problem: " & vbCrLf & identifier_msg & ":" & ex.ToString
                Else
                    Display_Message1.Message_Textbox.Text = "The Application encountered the following problem: " & vbCrLf & identifier_msg & ":" & ex.Message.ToString
                End If
                Display_Message1.Timer1.Interval = 1000
                Display_Message1.ShowDialog()
                Dim dir As System.IO.DirectoryInfo = New System.IO.DirectoryInfo((Application.StartupPath & "\").Replace("\\", "\") & "Error Logs")
                If dir.Exists = False Then
                    dir.Create()
                End If
                dir = Nothing
                Dim filewriter As System.IO.StreamWriter = New System.IO.StreamWriter((Application.StartupPath & "\").Replace("\\", "\") & "Error Logs\" & Format(Now(), "yyyyMMdd") & "_Error_Log.txt", True)
                filewriter.WriteLine("#" & Format(Now(), "dd/MM/yyyy hh:mm:ss tt") & " - " & identifier_msg & ":" & ex.ToString)
                filewriter.Flush()
                filewriter.Close()
                filewriter = Nothing
            End If
            ex = Nothing
            identifier_msg = Nothing
        Catch exc As Exception
            MsgBox("An error occurred in the application's error handling routine. The application will try to recover from this serious error.", MsgBoxStyle.Critical, "Critical Error Encountered")
        End Try
    End Sub


    Private Sub Status_Handler(ByVal Message As String)
        Try
            Status_Textbox.Text = Message.ToUpper
            Message = Nothing
        Catch ex As Exception
            Error_Handler(ex, "Status_Handler")
        End Try
    End Sub


    Private Sub Status_Results_Handler(ByVal Message As String)
        Try
            If Message.Length > 0 Then
                If (Message.Length + StatusResults_RichtextBox.Text.Length) > StatusResults_RichtextBox.MaxLength Then
                    StatusResults_RichtextBox.Clear()
                End If
                StatusResults_RichtextBox.AppendText(Message & vbCrLf)
                StatusResults_RichtextBox.Focus()
                StatusResults_RichtextBox.Select(StatusResults_RichtextBox.Text.Length - 1, 0)
                StatusResults_RichtextBox.ScrollToCaret()
            End If
            Message = Nothing
        Catch ex As Exception
            Error_Handler(ex, "Status_Results_Handler")
        End Try
    End Sub


    Private Function File_Exists(ByVal file_path As String) As Boolean
        Dim result As Boolean = False
        Try
            If Not file_path = "" And Not file_path Is Nothing Then
                Dim dinfo As FileInfo = New FileInfo(file_path)
                If dinfo.Exists = False Then
                    result = False
                Else
                    result = True
                End If
                dinfo = Nothing
            End If
            file_path = Nothing
        Catch ex As Exception
            Error_Handler(ex, "File_Exists")
        End Try
        Return result
    End Function

    Private Function Directory_Exists(ByVal directory_path As String) As Boolean
        Dim result As Boolean = False
        Try
            If Not directory_path = "" And Not directory_path Is Nothing Then
                Dim dinfo As DirectoryInfo = New DirectoryInfo(directory_path)
                If dinfo.Exists = False Then
                    result = False
                Else
                    result = True
                End If
                dinfo = Nothing
            End If
            directory_path = Nothing
        Catch ex As Exception
            Error_Handler(ex, "Directory_Exists")
        End Try
        Return result
    End Function

    Private Sub main_screen_formclosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        Try
            NotifyIcon1.Dispose()
            sender = Nothing
            e = Nothing
        Catch ex As Exception
            Error_Handler(ex, "main_screen_formclosing")
        End Try
    End Sub

    Private Sub main_screen_dblclick(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.DoubleClick
        Try
            hide_main_application()
            sender = Nothing
            e = Nothing
        Catch ex As Exception
            Error_Handler(ex, "main_screen_dblclick")
        End Try
    End Sub

    Private Sub Main_Screen_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Try
            Label1.Text = System.String.Format(Label1.Text, My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision)
            TimesExecuted = 0

            If Not My.Settings.InputTargetFolder_Textbox = Nothing Then
                InputTargetFolder_Textbox.Text = My.Settings.InputTargetFolder_Textbox
                InputTargetFolder_Textbox.Select(0, 0)
            Else
                InputTargetFolder_Textbox.Text = ""
            End If
            
            If Not My.Settings.Textbox1 = Nothing Then
                TextBox1.Text = My.Settings.Textbox1
                TextBox1.Select(0, 0)
            Else
                TextBox1.Text = ""
            End If
            

            Select Case My.Settings.FullErrors_Checkbox
                Case True
                    FullErrors_Checkbox.Checked = True
                    Exit Select
                Case False
                    FullErrors_Checkbox.Checked = False
                    Exit Select
                Case Else
                    FullErrors_Checkbox.Checked = True
                    Exit Select
            End Select

            inputseconds_label.Text = "4000"
            Timer1.Interval = 1000
            Timer1.Start()

            If InputTargetFolder_Textbox.Text.Length > 0 Then
                StartWorker()
            End If
            sender = Nothing
            e = Nothing
        Catch ex As Exception
            Error_Handler(ex, "Main_Screen_Load")
        End Try
    End Sub

    Private Sub Main_Screen_Close(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            ' Cancel the asynchronous operation.
            Me.BackgroundWorker1.CancelAsync()
            ' Disable the Cancel button.
            cancelAsyncButton.Enabled = False
            BackgroundWorker1.Dispose()

            NotifyIcon1.Dispose()

            My.Settings.FullErrors_Checkbox = FullErrors_Checkbox.Checked

            My.Settings.InputTargetFolder_Textbox = InputTargetFolder_Textbox.Text
            My.Settings.Textbox1 = TextBox1.Text

            My.Settings.Save()
        Catch ex As Exception
            Error_Handler(ex, "Main_Screen_Close")
        End Try

    End Sub




    Private Sub FullErrors_Checkbox_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FullErrors_Checkbox.CheckedChanged
        Status_Handler("Error Level Reporting Altered")
    End Sub

    Private Sub Browse_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Browse_Button.Click
        Status_Handler("Selecting Root Hand-in Folder")
        Try
            FolderBrowserDialog1.Description = "Select root hand-in folder:"
            If Directory_Exists(InputTargetFolder_Textbox.Text) = True Then
                FolderBrowserDialog1.SelectedPath = InputTargetFolder_Textbox.Text
            End If

            Dim result As DialogResult = FolderBrowserDialog1.ShowDialog
            If result = Windows.Forms.DialogResult.OK Then
                InputTargetFolder_Textbox.Text = FolderBrowserDialog1.SelectedPath
            End If

        Catch ex As Exception
            Error_Handler(ex, "Browse_Button_Click")
        End Try
        Status_Handler("Root Hand-in Folder Selected")
    End Sub




    Private Sub startAsyncButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles startAsyncButton.Click
        If InputTargetFolder_Textbox.Text.Length > 0 Then
            StartWorker()
        End If
        sender = Nothing
        e = Nothing
    End Sub


    Private Sub StartWorker()
        Try
            If busyworking = False Then

                StatusResults_RichtextBox.Clear()
                busyworking = True
                statusmessage = "Initializing Application for Operation Launch"
                Status_Handler(statusmessage)

                ' Reset the text in the result label.

                inputlines_label.Text = [String].Empty
                lastinputline_label.Text = [String].Empty
                datelaunched_label.Text = [String].Empty
                inputlines2_label.Text = [String].Empty
                inputlines2Total_label.Text = [String].Empty
                lastinputline2_label.Text = [String].Empty

                secondary_inputlinesprecount = 0
                secondary_inputlines = 0
                secondary_inputlinesTotal = 0
                secondary_lastinputline = ""
                secondary_lastinputlineFull = ""
                secondary_highestPercentageReached = 0
                secondary_PercentComplete = 0

                inputlines = 0
                lastinputline = ""
                lastinputlineFull = ""
                statusmessage = ""
                highestPercentageReached = 0
                inputlinesprecount = 0
                datelaunched = Now()
                pretestdone = False


                Controls_Enabler("run")


                ' Start the asynchronous operation.

                BackgroundWorker1.RunWorkerAsync(InputTargetFolder_Textbox.Text)
            End If
        Catch ex As Exception
            Error_Handler(ex, "StartWorker")
        End Try
    End Sub 'startAsyncButton_Click




    Private Sub cancelAsyncButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cancelAsyncButton.Click

        ' Cancel the asynchronous operation.
        Me.BackgroundWorker1.CancelAsync()

        ' Disable the Cancel button.
        cancelAsyncButton.Enabled = False
        sender = Nothing
        e = Nothing
    End Sub 'cancelAsyncButton_Click

    ' This event handler is where the actual work is done.
    Private Sub backgroundWorker1_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles BackgroundWorker1.DoWork

        ' Get the BackgroundWorker object that raised this event.
        Dim worker As BackgroundWorker = CType(sender, BackgroundWorker)

        ' Assign the result of the computation
        ' to the Result property of the DoWorkEventArgs
        ' object. This is will be available to the 
        ' RunWorkerCompleted eventhandler.
        e.Result = MainWorkerFunction(worker, e)
        sender = Nothing
        e = Nothing
        worker.Dispose()
        worker = Nothing
    End Sub 'backgroundWorker1_DoWork

    ' This event handler deals with the results of the
    ' background operation.
    Private Sub backgroundWorker1_RunWorkerCompleted(ByVal sender As Object, ByVal e As RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        busyworking = False
        ' First, handle the case where an exception was thrown.
        If Not (e.Error Is Nothing) Then
            Error_Handler(e.Error, "backgroundWorker1_RunWorkerCompleted")
        ElseIf e.Cancelled Then
            ' Next, handle the case where the user canceled the 
            ' operation.
            ' Note that due to a race condition in 
            ' the DoWork event handler, the Cancelled
            ' flag may not have been set, even though
            ' CancelAsync was called.
            Me.ProgressBar1.Value = 0
            inputlines_label.Text = "Cancelled"
            lastinputline_label.Text = "Cancelled"
            inputlines2_label.Text = "Cancelled"
            inputlines2Total_label.Text = "Cancelled"
            lastinputline2_label.Text = "Cancelled"
            Me.ToolTip1.SetToolTip(lastinputline_label, "Cancelled")
            Me.ToolTip1.SetToolTip(lastinputline2_label, "Cancelled")
            statusmessage = "Operation Cancelled"
            Status_Results_Handler("-- Operation Cancelled --")
        Else
            ' Finally, handle the case where the operation succeeded.

            Status_Handler(e.Result)

            Me.ProgressBar2.Value = 100
            Me.inputlines2_label.Text = secondary_inputlines & " (of " & secondary_inputlinesprecount & ")"
            Me.inputlines2Total_label.Text = secondary_inputlinesTotal
            Me.lastinputline2_label.Text = secondary_lastinputline
            Me.ToolTip1.SetToolTip(lastinputline2_label, secondary_lastinputlineFull)


            Me.ProgressBar1.Value = 100
            Me.inputlines_label.Text = inputlines & " (of " & inputlinesprecount & ")"
            Me.lastinputline_label.Text = lastinputline
            Me.ToolTip1.SetToolTip(lastinputline_label, lastinputlineFull)
            Me.datelaunched_label.Text = Format(datelaunched, "yyyy/MM/dd HH:mm:ss") & " - " & Format(Now, "yyyy/MM/dd HH:mm:ss") & " (" & Now.Subtract(Me.datelaunched).TotalSeconds() & " s)"

            statusmessage = "Operation Completed"
            Status_Results_Handler("-- Operation Completed --")
        End If

        Status_Handler(statusmessage)
        Controls_Enabler("stop")
        sender = Nothing
        e = Nothing

        TimesExecuted = TimesExecuted + 1
        If TimesExecuted = 11 Then
            Shell((Application.StartupPath & "\" & "Application_Loader.exe").Replace("\\", "\"), AppWinStyle.NormalFocus, False, -1)
            Me.Close()
        End If
        Label9.Text = (CInt(Label9.Text) - 1).ToString
    End Sub 'backgroundWorker1_RunWorkerCompleted

    Private Sub Controls_Enabler(ByVal action As String)
        Select Case action.ToLower
            Case "run"
                Me.InputTargetFolder_Textbox.Enabled = False
                Me.TextBox1.Enabled = False
                Me.Browse_Button.Enabled = False
                Me.startAsyncButton.Enabled = False

                ' Disable the Cancel button.
                Me.cancelAsyncButton.Enabled = True
                Exit Select
            Case "stop"
                Me.InputTargetFolder_Textbox.Enabled = True
                Me.TextBox1.Enabled = True
                Me.Browse_Button.Enabled = True
                Me.startAsyncButton.Enabled = True

                ' Disable the Cancel button.
                Me.cancelAsyncButton.Enabled = False
                Exit Select
            Case Else
                Me.InputTargetFolder_Textbox.Enabled = False
                Me.TextBox1.Enabled = False
                Me.Browse_Button.Enabled = False
                Me.startAsyncButton.Enabled = False

                ' Disable the Cancel button.
                Me.cancelAsyncButton.Enabled = True
                Exit Select
        End Select

        action = Nothing
    End Sub

    ' This event handler updates the progress bar.
    Private Sub backgroundWorker1_ProgressChanged(ByVal sender As Object, ByVal e As ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged

        Me.ProgressBar2.Value = secondary_PercentComplete
        inputlines2_label.Text = secondary_inputlines & " (of " & secondary_inputlinesprecount & ")"
        inputlines2Total_label.Text = secondary_inputlinesTotal
        lastinputline2_label.Text = secondary_lastinputline
        Me.ToolTip1.SetToolTip(lastinputline2_label, secondary_lastinputlineFull)

        Me.ProgressBar1.Value = e.ProgressPercentage
        inputlines_label.Text = inputlines & " (of " & inputlinesprecount & ")"
        lastinputline_label.Text = lastinputline
        Me.ToolTip1.SetToolTip(lastinputline_label, lastinputlineFull)

        datelaunched_label.Text = Format(datelaunched, "yyyy/MM/dd HH:mm:ss") & " - " & Format(Now, "yyyy/MM/dd HH:mm:ss") & " (" & Now.Subtract(Me.datelaunched).TotalSeconds() & " s)"
        If statusresults.Length > 0 Then
            Status_Results_Handler(statusresults.Trim)
        End If
        If statusmessage.Length > 0 Then
            Status_Handler(statusmessage)
        End If
        statusresults = ""
        statusmessage = ""
        sender = Nothing
        e = Nothing
    End Sub

    ' This is the method that does the actual work. 
    Function MainWorkerFunction(ByVal worker As BackgroundWorker, ByVal e As DoWorkEventArgs) As String
        Dim result As String = ""
        Try

            ' Abort the operation if the user has canceled.
            ' Note that a call to CancelAsync may have set 
            ' CancellationPending to true just after the
            ' last invocation of this method exits, so this 
            ' code will not have the opportunity to set the 
            ' DoWorkEventArgs.Cancel flag to true. This means
            ' that RunWorkerCompletedEventArgs.Cancelled will
            ' not be set to true in your RunWorkerCompleted
            ' event handler. This is a race condition.
            If worker.CancellationPending Then
                e.Cancel = True
            End If

            secondary_inputlinesprecount = 0
            secondary_inputlines = 0
            secondary_inputlinesTotal = 0
            secondary_lastinputline = ""
            secondary_lastinputlineFull = ""
            secondary_highestPercentageReached = 0

            If Me.pretestdone = False Then
                statusmessage = "Calculating Operation Parameters"
                primary_PercentComplete = 0
                statusresults = statusresults & "-- Operation Commenced --" & vbCrLf
                statusresults = statusresults & "Calculating Operation Parameters" & vbCrLf
                worker.ReportProgress(0)
                PreCount_Function()
                Me.pretestdone = True

            End If

            If worker.CancellationPending Then
                e.Cancel = True
            End If

            statusmessage = "Beginning Operation"
            statusresults = statusresults & "Beginning Operation" & vbCrLf
            statusresults = statusresults & "Generating HTML Files" & vbCrLf
            primary_PercentComplete = 0
            worker.ReportProgress(0)


            If My.Computer.FileSystem.DirectoryExists((Application.StartupPath & "\Generated").Replace("\\", "\")) = False Then
                My.Computer.FileSystem.CreateDirectory((Application.StartupPath & "\Generated").Replace("\\", "\"))
            End If



            Dim curdirectory As String = (Application.StartupPath & "\Generated").Replace("\\", "\")
            Dim curdir As DirectoryInfo = New DirectoryInfo(curdirectory)
            For Each delfile As FileInfo In curdir.GetFiles
                If delfile.Extension.ToLower = ".htm" Then
                    My.Computer.FileSystem.DeleteFile(delfile.FullName, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
                End If
                delfile = Nothing
            Next
            curdir = Nothing
            inputlines = 0
            If Directory_Exists(InputTargetFolder_Textbox.Text) = True Then
                Dim rootdir As DirectoryInfo = New DirectoryInfo(InputTargetFolder_Textbox.Text)
                Dim basedir As DirectoryInfo
                Dim filewriter As StreamWriter = New StreamWriter((curdirectory & "\default.htm").Replace("\\", "\"), False)
                filewriter.WriteLine("<html>")
                filewriter.WriteLine("<head>")
                filewriter.WriteLine("<link href=""Stylesheet.css"" type=""text/css"" rel=""STYLESHEET"">")
                filewriter.WriteLine("</head>")
                filewriter.WriteLine("<body>")
                filewriter.WriteLine("<p><img src=""header.jpg""></p>")
                filewriter.WriteLine("<h1>>> " & rootdir.Name & "</h1>")
                filewriter.WriteLine("<ul>")
                For Each basedir In rootdir.GetDirectories()

                    If worker.CancellationPending Then
                        e.Cancel = True
                        Exit For
                    End If

                    If basedir.Name.Length = 4 And IsNumeric(basedir.Name) = True Then
                        If Integer.Parse(basedir.Name) > Year(Now) - 1 Then
                            If basedir.Exists = True Then
                                If Not basedir.Name = "_Recycle Bin" Then
                                    filewriter.WriteLine("<li><a href=""" & basedir.Name & ".htm" & """>" & basedir.Name & "</a></li>")
                                End If

                                Dim filewriter2 As StreamWriter = New StreamWriter((curdirectory & "\" & basedir.Name & ".htm").Replace("\\", "\"), False)
                                filewriter2.WriteLine("<html>")
                                filewriter2.WriteLine("<head>")
                                filewriter2.WriteLine("<link href=""Stylesheet.css"" type=""text/css"" rel=""STYLESHEET"">")
                                filewriter2.WriteLine("</head>")
                                filewriter2.WriteLine("<body>")
                                filewriter2.WriteLine("<p><img src=""header.jpg""></p>")
                                filewriter2.WriteLine("<h1>>> " & rootdir.Name & " > " & basedir.Name & "</h1>")
                                filewriter2.WriteLine("<ul>")

                                Dim departmentdir As DirectoryInfo
                                For Each departmentdir In basedir.GetDirectories
                                    If worker.CancellationPending Then
                                        e.Cancel = True
                                        Exit For
                                    End If

                                    If Not departmentdir.Name = "_Recycle Bin" Then
                                        filewriter2.WriteLine("<li><a href=""" & basedir.Name & "_" & departmentdir.Name & ".htm" & """>" & departmentdir.Name & "</a></li>")
                                    End If

                                    If departmentdir.Exists = True Then

                                        Dim filewriter3 As StreamWriter = New StreamWriter((curdirectory & "\" & basedir.Name & "_" & departmentdir.Name & ".htm").Replace("\\", "\"), False)
                                        filewriter3.WriteLine("<html>")
                                        filewriter3.WriteLine("<head>")
                                        filewriter3.WriteLine("<link href=""Stylesheet.css"" type=""text/css"" rel=""STYLESHEET"">")
                                        filewriter3.WriteLine("</head>")
                                        filewriter3.WriteLine("<body>")
                                        filewriter3.WriteLine("<p><img src=""header.jpg""></p>")
                                        filewriter3.WriteLine("<h1>>> " & rootdir.Name & " > " & basedir.Name & " > " & departmentdir.Name & "</h1>")
                                        filewriter3.WriteLine("<ul>")


                                        Dim coursedir As DirectoryInfo
                                        For Each coursedir In departmentdir.GetDirectories
                                            If worker.CancellationPending Then
                                                e.Cancel = True
                                                Exit For
                                            End If

                                            If Not coursedir.Name = "_Recycle Bin" Then
                                                filewriter3.WriteLine("<li><a href=""" & basedir.Name & "_" & departmentdir.Name & "_" & coursedir.Name & ".htm" & """>" & coursedir.Name & "</a></li>")
                                            End If

                                            If coursedir.Exists = True Then

                                                Dim filewriter4 As StreamWriter = New StreamWriter((curdirectory & "\" & basedir.Name & "_" & departmentdir.Name & "_" & coursedir.Name & ".htm").Replace("\\", "\"), False)
                                                filewriter4.WriteLine("<html>")
                                                filewriter4.WriteLine("<head>")
                                                filewriter4.WriteLine("<link href=""Stylesheet.css"" type=""text/css"" rel=""STYLESHEET"">")
                                                filewriter4.WriteLine("</head>")
                                                filewriter4.WriteLine("<body>")
                                                filewriter4.WriteLine("<p><img src=""header.jpg""></p>")
                                                filewriter4.WriteLine("<h1>>> " & rootdir.Name & " > " & basedir.Name & " > " & departmentdir.Name & " > " & coursedir.Name & "</h1>")
                                                filewriter4.WriteLine("<ul>")

                                                Dim projectdir As DirectoryInfo
                                                For Each projectdir In coursedir.GetDirectories
                                                    If worker.CancellationPending Then
                                                        e.Cancel = True
                                                        Exit For
                                                    End If

                                                    If Not projectdir.Name = "_Recycle Bin" Then
                                                        filewriter4.WriteLine("<li><a href=""" & basedir.Name & "_" & departmentdir.Name & "_" & coursedir.Name & "_" & projectdir.Name & ".htm" & """>" & projectdir.Name & "</a></li>")
                                                    End If

                                                    lastinputline = projectdir.Name
                                                    lastinputlineFull = projectdir.FullName
                                                    Dim percentComplete As Integer
                                                    percentComplete = 0
                                                    If inputlinesprecount > 0 Then
                                                        percentComplete = CSng(inputlines) / CSng(inputlinesprecount) * 100
                                                    Else
                                                        percentComplete = 100
                                                    End If
                                                    primary_PercentComplete = percentComplete
                                                    If percentComplete > highestPercentageReached Then
                                                        highestPercentageReached = percentComplete
                                                        statusmessage = "Processing Handin Folder"
                                                        worker.ReportProgress(percentComplete)
                                                    End If

                                                    If projectdir.Exists = True Then

                                                        Dim filewriter5 As StreamWriter = New StreamWriter((curdirectory & "\" & basedir.Name & "_" & departmentdir.Name & "_" & coursedir.Name & "_" & projectdir.Name & ".htm").Replace("\\", "\"), False)
                                                        filewriter5.WriteLine("<html>")
                                                        filewriter5.WriteLine("<head>")
                                                        filewriter5.WriteLine("<link href=""Stylesheet.css"" type=""text/css"" rel=""STYLESHEET"">")
                                                        filewriter5.WriteLine("</head>")
                                                        filewriter5.WriteLine("<body>")
                                                        filewriter5.WriteLine("<p><img src=""header.jpg""></p>")
                                                        filewriter5.WriteLine("<h1>>> " & rootdir.Name & " > " & basedir.Name & " > " & departmentdir.Name & " > " & coursedir.Name & " > " & projectdir.Name & "</h1>")
                                                        filewriter5.WriteLine("<table width=""100%""><tr>")
                                                        filewriter5.WriteLine("<td width=""70%"" align=""left"" valign=""top"">")
                                                        filewriter5.WriteLine("<ul>")

                                                        Dim dowork As Boolean = False
                                                        Dim worktodo As String = "remove"
                                                        Dim accessgate As String = "close"


                                                        'Activity_Handler(projectdir.FullName & ": Access Gate is " & accessgate)
                                                        'statusresults = statusresults & (projectdir.Name & ": Access Gate is " & accessgate).Trim.Trim.Replace(vbCrLf, " ") & vbCrLf


                                                        secondary_inputlinesprecount = 0
                                                        secondary_inputlines = 0

                                                        'secondary_lastinputline = ""
                                                        secondary_highestPercentageReached = 0

                                                        secondary_inputlinesprecount = projectdir.GetDirectories.Length

                                                        Dim studfiles As Long = 0
                                                        Dim studdirs As Long = 0
                                                        Dim stud As Long = 0

                                                        Dim studentdir As DirectoryInfo
                                                        For Each studentdir In projectdir.GetDirectories
                                                            If worker.CancellationPending Then
                                                                e.Cancel = True
                                                                Exit For
                                                            End If

                                                            If Not studentdir.Name = "_Recycle Bin" Then
                                                                stud = stud + 1
                                                                Dim cstudfiles As Long = 0
                                                                Dim cstuddirs As Long = 0
                                                                filewriter5.WriteLine("<li><b>" & studentdir.Name & "</b></li>")
                                                                filewriter5.WriteLine("<ul>")
                                                                Dim subtractamount As Integer = 0
                                                                If My.Computer.FileSystem.DirectoryExists((studentdir.FullName & "\_Recycle Bin").Replace("\\", "\")) Then
                                                                    subtractamount = 1
                                                                End If
                                                                filewriter5.WriteLine("<li>Directories: " & studentdir.GetDirectories.Length - subtractamount & "<br>")
                                                                studdirs = studdirs + studentdir.GetDirectories.Length - subtractamount
                                                                cstuddirs = cstuddirs + studentdir.GetDirectories.Length - subtractamount
                                                                For Each fi As DirectoryInfo In studentdir.GetDirectories
                                                                    If Not fi.Name = "_Recycle Bin" Then
                                                                        filewriter5.WriteLine("<i>" & fi.Name & " [D:" & fi.GetDirectories.Length & "][F:" & fi.GetFiles.Length & "]</i>")
                                                                        studfiles = studfiles + fi.GetFiles.Length
                                                                        studdirs = studdirs + fi.GetDirectories.Length
                                                                        cstudfiles = cstudfiles + fi.GetFiles.Length
                                                                        cstuddirs = cstuddirs + fi.GetDirectories.Length
                                                                        filewriter5.WriteLine(" | ")
                                                                    End If
                                                                Next
                                                                filewriter5.WriteLine("</li>")
                                                                filewriter5.WriteLine("<li>Files: " & studentdir.GetFiles.Length & "<br>")
                                                                studfiles = studfiles + studentdir.GetFiles.Length
                                                                cstudfiles = cstudfiles + studentdir.GetFiles.Length
                                                                For Each fi As FileInfo In studentdir.GetFiles
                                                                    filewriter5.WriteLine("<i>" & fi.Name & "</i> | ")
                                                                Next
                                                                filewriter5.WriteLine("</li>")
                                                                filewriter5.WriteLine("<li>")
                                                                If cstudfiles > 0 Or cstuddirs > 0 Then
                                                                    filewriter5.WriteLine("<font color=""#008800"">Handin Appears Successful</font>")
                                                                Else
                                                                    filewriter5.WriteLine("<font color=""#FF0000"">No Handin Detected</font>")
                                                                End If
                                                                filewriter5.WriteLine("</li>")
                                                                filewriter5.WriteLine("</ul>")
                                                            End If

                                                            secondary_PercentComplete = 0
                                                            secondary_lastinputline = studentdir.Name
                                                            secondary_lastinputlineFull = studentdir.FullName
                                                            ' Report progress as a percentage of the total task.
                                                            If secondary_inputlinesprecount > 0 Then
                                                                secondary_PercentComplete = CSng(secondary_inputlines) / CSng(secondary_inputlinesprecount) * 100
                                                            Else
                                                                secondary_PercentComplete = 100
                                                            End If

                                                            If secondary_PercentComplete > secondary_highestPercentageReached Then
                                                                secondary_highestPercentageReached = secondary_PercentComplete
                                                                statusmessage = "Processing Handin Folder"
                                                                worker.ReportProgress(primary_PercentComplete)
                                                            End If




                                                            secondary_inputlines = secondary_inputlines + 1
                                                            secondary_inputlinesTotal = secondary_inputlinesTotal + 1
                                                            secondary_lastinputline = studentdir.Name
                                                            secondary_lastinputlineFull = studentdir.FullName
                                                            secondary_PercentComplete = 0
                                                            ' Report progress as a percentage of the total task.
                                                            If secondary_inputlinesprecount > 0 Then
                                                                secondary_PercentComplete = CSng(secondary_inputlines) / CSng(secondary_inputlinesprecount) * 100
                                                            Else
                                                                secondary_PercentComplete = 100
                                                            End If

                                                            If secondary_PercentComplete > secondary_highestPercentageReached Then
                                                                secondary_highestPercentageReached = secondary_PercentComplete
                                                                statusmessage = "Processing Handin Folder"
                                                                worker.ReportProgress(primary_PercentComplete)
                                                            End If
                                                        Next
                                                        studentdir = Nothing

                                                        filewriter5.WriteLine("</ul>")
                                                        filewriter5.WriteLine("</td>")
                                                        filewriter5.WriteLine("<td width=""30%"" align=""left"" valign=""top"">")
                                                        filewriter5.WriteLine("<p>Students: " & stud & "</p>")
                                                        filewriter5.WriteLine("<p>Approx. Files: " & studfiles & " <i>[Primary Level Scan]</i><br>")
                                                        filewriter5.WriteLine("Approx. Folders: " & studdirs & " <i>[Primary Level Scan]</i></p>")
                                                        filewriter5.WriteLine("</td>")
                                                        filewriter5.WriteLine("</tr></table>")
                                                        filewriter5.WriteLine("<br><p><font color=""#5A6C71"">This summary page was generated at " & Format(Now(), "HH:mm:ss dd/MM/yyyy") & " by Student Handin Project Summary Creator (Build " & System.String.Format("{0}{1:00}{2:00}.{3}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision) & ")</font></p>")
                                                        filewriter5.WriteLine("</body>" & vbCrLf & "</html>")
                                                        filewriter5.Flush()
                                                        filewriter5.Close()
                                                        filewriter5 = Nothing





                                                    End If




                                                    lastinputline = projectdir.Name
                                                    lastinputlineFull = projectdir.FullName
                                                    inputlines = inputlines + 1

                                                    ' Report progress as a percentage of the total task.
                                                    percentComplete = 0
                                                    If inputlinesprecount > 0 Then
                                                        percentComplete = CSng(inputlines) / CSng(inputlinesprecount) * 100
                                                    Else
                                                        percentComplete = 100
                                                    End If
                                                    primary_PercentComplete = percentComplete
                                                    If percentComplete > highestPercentageReached Then
                                                        highestPercentageReached = percentComplete
                                                        statusmessage = "Processing Handin Folder"
                                                        worker.ReportProgress(percentComplete)
                                                    End If

                                                Next
                                                projectdir = Nothing

                                                filewriter4.WriteLine("</ul>")
                                                filewriter4.WriteLine("<br><p><font color=""#5A6C71"">This summary page was generated at " & Format(Now(), "HH:mm:ss dd/MM/yyyy") & " by Student Handin Project Summary Creator (Build " & System.String.Format("{0}{1:00}{2:00}.{3}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision) & ")</font></p>")
                                                filewriter4.WriteLine("</body>" & vbCrLf & "</html>")
                                                filewriter4.Flush()
                                                filewriter4.Close()
                                                filewriter4 = Nothing
                                            End If
                                        Next
                                        coursedir = Nothing

                                        filewriter3.WriteLine("</ul>")
                                        filewriter3.WriteLine("<br><p><font color=""#5A6C71"">This summary page was generated at " & Format(Now(), "HH:mm:ss dd/MM/yyyy") & " by Student Handin Project Summary Creator (Build " & System.String.Format("{0}{1:00}{2:00}.{3}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision) & ")</font></p>")
                                        filewriter3.WriteLine("</body>" & vbCrLf & "</html>")
                                        filewriter3.Flush()
                                        filewriter3.Close()
                                        filewriter3 = Nothing
                                    End If
                                Next
                                departmentdir = Nothing

                                filewriter2.WriteLine("</ul>")
                                filewriter2.WriteLine("<br><p><font color=""#5A6C71"">This summary page was generated at " & Format(Now(), "HH:mm:ss dd/MM/yyyy") & " by Student Handin Project Summary Creator (Build " & System.String.Format("{0}{1:00}{2:00}.{3}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision) & ")</font></p>")
                                filewriter2.WriteLine("</body>" & vbCrLf & "</html>")
                                filewriter2.Flush()
                                filewriter2.Close()
                                filewriter2 = Nothing
                            End If
                        End If
                    End If
                Next
                filewriter.WriteLine("</ul>")
                filewriter.WriteLine("<br><p><font color=""#5A6C71"">This summary page was generated at " & Format(Now(), "HH:mm:ss dd/MM/yyyy") & " by Student Handin Project Summary Creator (Build " & System.String.Format("{0}{1:00}{2:00}.{3}", My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.Build, My.Application.Info.Version.Revision) & ")</font></p>")
                filewriter.WriteLine("</body>" & vbCrLf & "</html>")
                filewriter.Flush()
                filewriter.Close()
                filewriter = Nothing
                basedir = Nothing
                rootdir = Nothing

                If worker.CancellationPending Then
                    e.Cancel = True

                Else

                    If TextBox1.Text.Length > 0 Then

                        If My.Computer.FileSystem.DirectoryExists(TextBox1.Text) Then
                            statusresults = statusresults & "Generating HTML Files" & vbCrLf
                            statusmessage = "Moving files to Web Server"
                            statusresults = statusresults & "Removing Existing Files from Web Server" & vbCrLf
                            worker.ReportProgress(100)

                            Dim curdir2 As DirectoryInfo = New DirectoryInfo(TextBox1.Text)
                            For Each delfile As FileInfo In curdir2.GetFiles
                                Try
                                    If worker.CancellationPending Then
                                        e.Cancel = True
                                        Exit For
                                    End If
                                    If delfile.Extension.ToLower = ".htm" Then
                                        'statusresults = statusresults & (projectdir.Name & ": Access Gate is " & accessgate).Trim.Trim.Replace(vbCrLf, " ") & vbCrLf
                                        My.Computer.FileSystem.DeleteFile(delfile.FullName, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.DeletePermanently)
                                        statusresults = statusresults & "Removing: " & delfile.Name & vbCrLf
                                        worker.ReportProgress(100)
                                    End If
                                    delfile = Nothing

                                Catch ex As Exception
                                    Error_Handler(ex, "MainWorkerFunction - Web Folder Delete Files")
                                    Exit For
                                End Try
                            Next
                            statusresults = statusresults & "Removing Existing Files from Web Server Completed" & vbCrLf
                            worker.ReportProgress(100)
                            Dim curdir3 As DirectoryInfo = New DirectoryInfo(curdirectory)
                            statusresults = statusresults & "Moving Files to Web Server" & vbCrLf
                            worker.ReportProgress(100)
                            For Each delfile As FileInfo In curdir3.GetFiles
                                Try
                                    If worker.CancellationPending Then
                                        e.Cancel = True
                                        Exit For
                                    End If
                                    My.Computer.FileSystem.CopyFile(delfile.FullName, (curdir2.FullName & "\" & delfile.Name).Replace("\\", "\"), True)
                                    statusresults = statusresults & "Moving: " & delfile.Name & vbCrLf
                                    delfile = Nothing
                                Catch ex As Exception
                                    Error_Handler(ex, "MainWorkerFunction - Web Folder Copy Files")
                                    Exit For
                                End Try
                            Next
                            statusresults = statusresults & "Moving Files to Web Server Completed" & vbCrLf
                            worker.ReportProgress(100)
                            curdir2 = Nothing
                            curdir3 = Nothing
                        End If
                    End If
                End If
            Else
                Dim Display_Message1 As New Display_Message()
                Display_Message1.Message_Textbox.Text = "Connection to the Root Directory cannot be established."
                Display_Message1.Timer1.Interval = 1000
                Display_Message1.ShowDialog()
            End If


        Catch ex As Exception
            Error_Handler(ex, "MainWorkerFunction")
        End Try
        worker.Dispose()
        worker = Nothing
        e = Nothing
        Return result

    End Function

    Private Sub PreCount_Function()
        Try
            inputlinesprecount = 0

            If Directory_Exists(InputTargetFolder_Textbox.Text) = True Then
                Dim rootdir As DirectoryInfo = New DirectoryInfo(InputTargetFolder_Textbox.Text)
                Dim basedir As DirectoryInfo
                For Each basedir In rootdir.GetDirectories()
                    If basedir.Name.Length = 4 And IsNumeric(basedir.Name) = True Then
                        If Integer.Parse(basedir.Name) > Year(Now) - 1 Then
                            If basedir.Exists = True Then
                                Dim departmentdir As DirectoryInfo
                                For Each departmentdir In basedir.GetDirectories
                                    If departmentdir.Exists = True Then
                                        Dim coursedir As DirectoryInfo
                                        For Each coursedir In departmentdir.GetDirectories
                                            If coursedir.Exists = True Then
                                                Dim projectdir As DirectoryInfo
                                                For Each projectdir In coursedir.GetDirectories
                                                    inputlinesprecount = inputlinesprecount + 1
                                                Next
                                                projectdir = Nothing
                                            End If
                                        Next
                                        coursedir = Nothing
                                    End If
                                Next
                                departmentdir = Nothing
                            End If
                        End If
                    End If
                Next
                basedir = Nothing
                rootdir = Nothing
            End If

        Catch ex As Exception
            Error_Handler(ex, "PreCount_Function")
        End Try
    End Sub











    Private Sub ToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripMenuItem1.Click
        StatusResults_RichtextBox.Clear()
    End Sub


    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Try
            inputseconds_label.Text = (Integer.Parse(inputseconds_label.Text) - 1).ToString
            If Integer.Parse(inputseconds_label.Text) = 0 Then
                'do action
                StartWorker()
                inputseconds_label.Text = "4000"
            End If
            sender = Nothing
            e = Nothing
        Catch ex As Exception
            Error_Handler(ex, "Timer1_Tick")
        End Try
    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        If Not Me.WindowState = FormWindowState.Normal Then
            show_main_application()
        Else
            hide_main_application()
        End If

    End Sub

    Private Sub ExitApplicationToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitApplicationToolStripMenuItem.Click
        Try
            Me.Close()
        Catch ex As Exception
            Error_Handler(ex, "ExitApplicationToolStripMenuItem_Click")
        End Try
    End Sub

    Private Sub DisplayApplicationToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DisplayApplicationToolStripMenuItem.Click
        Try
            show_main_application()
        Catch ex As Exception
            Error_Handler(ex, "DisplayApplicationToolStripMenuItem_Click")
        End Try
    End Sub

    Private Sub show_main_application()
        Try
            Me.Opacity = 1
            Me.BringToFront()
            Me.Refresh()
            Me.WindowState = FormWindowState.Normal
            'Me.WindowState = FormWindowState.Normal
            'Me.Visible = True
            'Me.Opacity = 100
        Catch ex As Exception
            Error_Handler(ex, "show_main_application")
        End Try
    End Sub

    Private Sub hide_main_application()
        Try
            Me.WindowState = FormWindowState.Minimized
            If Me.WindowState = FormWindowState.Minimized Then
                NotifyIcon1.Visible = True
                Me.Opacity = 0
            End If
            'Me.WindowState = FormWindowState.Minimized
            'Me.Visible = False
            'Me.Opacity = 0
        Catch ex As Exception
            Error_Handler(ex, "hide_main_application")
        End Try
    End Sub

    Private Sub ToolStripMenuItem2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripMenuItem2.Click
        Try
            hide_main_application()
        Catch ex As Exception
            Error_Handler(ex, "ToolStripMenuItem2_Click")
        End Try
    End Sub

    Private Sub Label8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Label8.Click
        hide_main_application()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Status_Handler("Selecting Web Server Folder")
        Try
            FolderBrowserDialog1.Description = "Select web server folder:"
            If Directory_Exists(TextBox1.Text) = True Then
                FolderBrowserDialog1.SelectedPath = TextBox1.Text
            End If

            Dim result As DialogResult = FolderBrowserDialog1.ShowDialog
            If result = Windows.Forms.DialogResult.OK Then
                TextBox1.Text = FolderBrowserDialog1.SelectedPath
            End If

        Catch ex As Exception
            Error_Handler(ex, "Browse_Button_Click")
        End Try
        Status_Handler("Web Server Folder Selected")
    End Sub
End Class
