Imports Geometry3D
Imports System.IO
Imports System.Drawing.Drawing2D
Imports System

Imports System.Windows.Forms

Imports AdvanceMath

Imports System.Runtime.InteropServices
Public Class Form1
#Region "3D"

    Public zoomfactor As Single = 1

    Dim Axis(3) As Segment3D
    ' Dim yAxis As Segment3D
    ' Dim zAxis As Segment3D

    Dim Viewtheta, Viewfi As Single
    Dim Focus3D As Point3D = Origin3D()

    Dim LastMousePosition As Point

    Dim pt3dC(7) As Point3D
    ' Dim Centre As Point3D

    Dim OrbitRadius As Single
    Dim OrbitSpeed As Single
    Dim Cam3D As New Camera3D
    Private grxhi As Long, gryhi As Long 'Number of grids

    Const LStep As Long = 15 ' Need 360\LStep = integer
    Const zpi# = 3.1415927
    Const dtr# = zpi# / 180

    Private Property Font As System.Drawing.Font
    Private Rw As Single

#Region "paste & copy "

    Public Sub PopulateDataGridView()
        DataGridView1.ColumnCount = 6

        DataGridView1.Columns(0).Name = "C0"
        DataGridView1.Columns(1).Name = "C1"
        DataGridView1.Columns(2).Name = "C2"
        DataGridView1.Columns(3).Name = "C3"
        DataGridView1.Columns(4).Name = "C4"
        DataGridView1.Columns(5).Name = "C5"

        DataGridView1.Columns(0).HeaderText = "C0"
        DataGridView1.Columns(1).HeaderText = "C1"
        DataGridView1.Columns(2).HeaderText = "C2"
        DataGridView1.Columns(3).HeaderText = "C3"
        DataGridView1.Columns(4).HeaderText = "C4"
        DataGridView1.Columns(5).HeaderText = "C5"

        DataGridView1.Columns(0).Width = 40
        DataGridView1.Columns(1).Width = 40
        DataGridView1.Columns(2).Width = 40
        DataGridView1.Columns(3).Width = 40
        DataGridView1.Columns(4).Width = 40
        DataGridView1.Columns(5).Width = 40

        DataGridView1.Rows.Add(New String() {"A1", "B1", "C1", "D1", "E1", "F1"})
        DataGridView1.Rows.Add(New String() {"A2", "B2", "C2", "D2", "E2", "F2"})
        DataGridView1.Rows.Add(New String() {"A3", "B3", "C3", "D3", "E3", "F3"})
        DataGridView1.Rows.Add(New String() {"A4", "B4", "C4", "D4", "E4", "F4"})
        DataGridView1.Rows.Add(New String() {"A5", "B5", "C5", "D5", "E5", "F5"})
        DataGridView1.Rows.Add(New String() {"A6", "B6", "C6", "D6", "E6", "F6"})
        DataGridView1.Rows.Add(New String() {"A7", "B7", "C7", "D7", "E7", "F7"})

        DataGridView1.AutoResizeColumns()
    End Sub

    Private Sub DataGridView1_CellMouseClick(ByVal sender As Object, ByVal e As DataGridViewCellMouseEventArgs) Handles DataGridView1.CellMouseClick
        If DataGridView1.SelectedCells.Count > 0 Then
            DataGridView1.ContextMenuStrip = ContextMenuStrip
        End If
    End Sub

    Private Sub cutToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles CutToolStripMenuItem.Click
        'Copy to clipboard
        CopyToClipboard()

        'Clear selected cells
        For Each dgvCell As DataGridViewCell In DataGridView1.SelectedCells
            dgvCell.Value = String.Empty
        Next
    End Sub

    Private Sub copyToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles CopyToolStripMenuItem.Click
        CopyToClipboard()
    End Sub

    Private Sub pasteToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles PasteToolStripMenuItem.Click
        'Perform paste Operation
        PasteClipboardValue()
    End Sub

    Private Sub DataGridView1_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles DataGridView1.KeyDown
        Try
            If e.Modifiers = Keys.Control Then
                Select Case e.KeyCode
                    Case Keys.C
                        CopyToClipboard()
                        Exit Select

                    Case Keys.V
                        PasteClipboardValue()
                        Exit Select
                End Select
            End If
        Catch ex As Exception
            MessageBox.Show("Copy/paste operation failed. " + ex.Message, "Copy/Paste", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub CopyToClipboard()
        'Copy to clipboard
        Dim dataObj As DataObject = DataGridView1.GetClipboardContent()
        If dataObj IsNot Nothing Then
            Clipboard.SetDataObject(dataObj)
        End If
    End Sub

    Private Sub PasteClipboardValue()
        'Show Error if no cell is selected
        If DataGridView1.SelectedCells.Count = 0 Then
            MessageBox.Show("Please select a cell", "Paste", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        'Get the satring Cell
        Dim startCell As DataGridViewCell = GetStartCell(DataGridView1)
        'Get the clipboard value in a dictionary
        Dim cbValue As Dictionary(Of Integer, Dictionary(Of Integer, String)) = ClipBoardValues(Clipboard.GetText())

        Dim iRowIndex As Integer = startCell.RowIndex
        For Each rowKey As Integer In cbValue.Keys
            Dim iColIndex As Integer = startCell.ColumnIndex
            For Each cellKey As Integer In cbValue(rowKey).Keys
                'Check if the index is with in the limit
                If iColIndex <= DataGridView1.Columns.Count - 1 AndAlso iRowIndex <= DataGridView1.Rows.Count - 1 Then
                    Dim cell As DataGridViewCell = DataGridView1(iColIndex, iRowIndex)

                    'Copy to selected cells if 'chkPasteToSelectedCells' is checked
                    If (chkPasteToSelectedCells.Checked AndAlso cell.Selected) OrElse (Not chkPasteToSelectedCells.Checked) Then
                        cell.Value = cbValue(rowKey)(cellKey)
                    End If
                End If
                iColIndex += 1
            Next
            iRowIndex += 1
        Next
    End Sub

    Private Function GetStartCell(ByVal dgView As DataGridView) As DataGridViewCell
        'get the smallest row,column index
        If dgView.SelectedCells.Count = 0 Then
            Return Nothing
        End If

        Dim rowIndex As Integer = dgView.Rows.Count - 1
        Dim colIndex As Integer = dgView.Columns.Count - 1

        For Each dgvCell As DataGridViewCell In dgView.SelectedCells
            If dgvCell.RowIndex < rowIndex Then
                rowIndex = dgvCell.RowIndex
            End If
            If dgvCell.ColumnIndex < colIndex Then
                colIndex = dgvCell.ColumnIndex
            End If
        Next

        Return dgView(colIndex, rowIndex)
    End Function

    Private Function ClipBoardValues(ByVal clipboardValue As String) As Dictionary(Of Integer, Dictionary(Of Integer, String))
        Dim copyValues As New Dictionary(Of Integer, Dictionary(Of Integer, String))()

        Dim lines As [String]() = clipboardValue.Split(ControlChars.Lf)

        For i As Integer = 0 To lines.Length - 1
            copyValues(i) = New Dictionary(Of Integer, String)()
            Dim lineContent As [String]() = lines(i).Split(ControlChars.Tab)

            'if an empty cell value copied, then set the dictionay with an empty string
            'else Set value to dictionary
            If lineContent.Length = 0 Then
                copyValues(i)(0) = String.Empty
            Else
                For j As Integer = 0 To lineContent.Length - 1
                    copyValues(i)(j) = lineContent(j)
                Next
            End If
        Next
        Return copyValues
    End Function

#End Region

    Private Structure View2DParam
        Dim Centre As Point
    End Structure
    Private Sub PictureBox1_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PictureBox1.MouseDown
        LastMousePosition.X = e.X
        LastMousePosition.Y = e.Y
        ' DataGridView1.Rows.Add({e.X - 100, e.Y - 100, "0", e.X + 100, e.Y + 100, "0"})
        StopAutorotation()
    End Sub
    Private View2D As View2DParam

    Private Sub PictureBox1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles PictureBox1.MouseMove

        ' ListBox1.Items.Add("0 " & "0 " & "0 " & e.X & " " & e.Y & "0 " & "00")
        If e.Button = Windows.Forms.MouseButtons.Right Then

            View2D.Centre.X = View2D.Centre.X + (e.X - LastMousePosition.X)
            View2D.Centre.Y = View2D.Centre.Y + (e.Y - LastMousePosition.Y)
            Me.Invalidate()
        End If
        If e.Button = Windows.Forms.MouseButtons.Middle Then

            Viewfi = Viewfi - (e.Y - LastMousePosition.Y) * OrbitSpeed
            : Viewtheta = Viewtheta - (e.X - LastMousePosition.X) * OrbitSpeed
            : If Viewtheta > 2 * PI Then Viewtheta = Viewtheta - (2 * PI)
            : If Viewtheta < -2 * PI Then Viewtheta = Viewtheta + (2 * PI)
            ' If Viewfi > 2 * PI Then Viewfi = Viewfi - (2 * PI)
            'If Viewfi < -2 * PI Then Viewfi = Viewfi + (2 * PI)
            : If Viewfi > PI Then Viewfi = -(PI)
            : If Viewfi < -PI Then Viewfi = PI
            Cam3D.RotateCamera((e.X - LastMousePosition.X) * 0.3, (e.Y - LastMousePosition.Y) * 0.3)

            ' If Viewfi >= (PI / 2) Then Viewfi = (PI / 2)
            ' If Viewfi <= -(PI / 2) Then Viewfi = -(PI / 2)
            : LastMousePosition.X = e.X
            : LastMousePosition.Y = e.Y
            Me.Invalidate()
        End If
        If chn = False Then
            chn = True

        End If
        LastMousePosition.X = e.X
        LastMousePosition.Y = e.Y

    End Sub
    Public g000 As Graphics

    Protected Overrides Sub OnPaint(ByVal e As System.Windows.Forms.PaintEventArgs)
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        Dim alpha As Long
        alpha = 255

        PictureBox1.Refresh()
        If CheckBox5.Checked = True Then
            ' _3DCanvas1.Invalidate()
            ' g000.Clear(Color.FromArgb(242, 242, 242))

            PictureBox1.CreateGraphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            PictureBox1.Refresh()

            ' gxs = _3DCanvas1.CreateGraphics
            ' Dim cube3d As New Object3D
            ' cube3d = MakeCube(300, 300, 900)

            ' object1 = cube3d

            ' cube3d.drawMode = DrawStyle.SOLID
            ' DrawObject3D(object1)
            ' DrawObject3D(cube3d)
            sd()
            Drawaxis()
            g000 = PictureBox1.CreateGraphics
        End If
    End Sub
    Public Sub Drawaxis()
        Dim col As Color
        Dim pt1 As Point3D
        For i = 0 To 2
            If i = 0 Then col = Color.Red 'x
            If i = 1 Then col = Color.Green 'y
            If i = 2 Then col = Color.Blue 'z

            ' g.DrawString(Cam3D.Theta.ToString & ":" & Cam3D.Fi.ToString, Me.Font, New SolidBrush(Color.Coral), New PointF(10, 20))
            g000.DrawString(" X = " & Rw * Sin(Viewfi) * Cos(Viewtheta).ToString & " : " & " Y = " & Rw * Sin(Viewfi) * Sin(Viewtheta).ToString & " : " & _
            " Z = " & Rw * Cos(Viewfi).ToString, PictureBox1.Font, New SolidBrush(Color.Coral), New PointF(10, 10))
            drawline(Axis(i).Pt1, Axis(i).Pt2, col)
        Next
        ' drawcube()
        Dim Axisf(2) As Segment3D
        For i = 0 To HDcount
            Axisf(0) = New Segment3D(New Point3D(HD1(i).x * zoomfactor, HD1(i).y * zoomfactor, HD1(i).z * zoomfactor), New Point3D(HD1(i).x1 * zoomfactor, HD1(i).y1 * zoomfactor, HD1(i).z1 * zoomfactor))

            drawline(Axisf(0).Pt1, Axisf(0).Pt2, Color.Black)
        Next
        : list()
        net()

        ' DrawWithoutHidden()
    End Sub

    Dim chn As Boolean = False
    Sub drawline(ByVal pt1 As Point3D, ByVal pt2 As Point3D, Optional ByVal col As Color = Nothing, Optional ByVal width As Integer = 1)
        Dim pt2d1, pt2d2 As Point
        Dim pt3d As Point3D

        If col = Nothing Then col = Color.FromArgb(180, 80, 100)

        pt3d = Cam3D.Project3Dto2D(New Geometry3D.Point3D(pt1.x, pt1.y, pt1.z))
        pt2d1.X = pt3d.x + View2D.Centre.X + 200
        pt2d1.Y = -pt3d.y + View2D.Centre.Y + 200

        pt3d = Cam3D.Project3Dto2D(New Geometry3D.Point3D(pt2.x, pt2.y, pt2.z))

        pt2d2.X = pt3d.x + View2D.Centre.X + 200
        pt2d2.Y = -pt3d.y + View2D.Centre.Y + 200

        g000.DrawLine(New Pen(col, width), pt2d1, pt2d2)

        Return
    End Sub

    Const HD As Integer = 10000 'Set these as applicable
    Dim HD1(HD + 2) As Hd3d 'Our points, the extra 2 are for the 3 points of the supertriangle
    Dim HDcount As Integer = -1
    Private Structure Hd3d
        Dim x As Single
        Dim y As Single
        Dim z As Single
        Dim x1 As Single
        Dim y1 As Single
        Dim z1 As Single
    End Structure
    Public Sub Add(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal x1 As Single, ByVal y1 As Single, ByVal z1 As Single)

        HDcount += 1
        HD1(HDcount).x = x
        HD1(HDcount).y = y
        HD1(HDcount).z = z
        HD1(HDcount).x1 = x1
        HD1(HDcount).y1 = y1
        HD1(HDcount).z1 = z1

    End Sub

    Sub list()
        On Error Resume Next
        If CheckBox2.Checked = True Then
        Else
            HDcount = -1
        End If

        If CheckBox8.Checked Then

            For i As Integer = 0 To ListBox2.Items.Count - 1

                Dim intSpacePos As Integer
                Dim strPoint As String = ListBox2.Items.Item(i)
                Dim t As String
                Dim r As Integer
                Dim u As Integer
                Dim y As Integer
                Dim y1 As Integer
                Dim y2 As Integer
                Dim y3 As String

                intSpacePos = InStr(strPoint, " ")
                t = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                intSpacePos = InStr(strPoint, " ")
                r = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                intSpacePos = InStr(strPoint, " ")
                u = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                intSpacePos = InStr(strPoint, " ")
                y = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                intSpacePos = InStr(strPoint, " ")
                y1 = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                intSpacePos = InStr(strPoint, " ")
                y2 = Trim(Mid(strPoint, 1, intSpacePos)) * zoomfactor
                strPoint = Trim(Mid(strPoint, Trim(intSpacePos)))

                y3 = Trim(Mid(strPoint, 1, intSpacePos))
                Dim Axisf(2) As Segment3D
                Axisf(0) = New Segment3D(New Point3D(t, r, u), New Point3D(y, y1, y2))

                drawline(Axisf(0).Pt1, Axisf(0).Pt2, Color.Black)

            Next
        Else
            For w As Integer = 0 To DataGridView1.RowCount - 1

                Dim Axisf(2) As Segment3D
                Dim t As Integer = DataGridView1.Item(0, w).Value
                Dim r As Integer = DataGridView1.Item(1, w).Value
                Dim u As Integer = DataGridView1.Item(2, w).Value
                Dim y As Integer = DataGridView1.Item(3, w).Value
                Dim y1 As Integer = DataGridView1.Item(4, w).Value
                Dim y2 As Integer = DataGridView1.Item(5, w).Value

                Add(t, r, u, y, y1, y2)

            Next
        End If
    End Sub

    Sub net()

        If CheckBox1.Checked = True Then
            For i As Integer = 1 To 10

                Dim Axi(2) As Segment3D
                Axi(1) = New Segment3D(New Point3D(30 * zoomfactor, 30 * i * zoomfactor, 0), New Point3D(300 * zoomfactor, 30 * i * zoomfactor, 0))
                Axi(2) = New Segment3D(New Point3D(30 * i * zoomfactor, 30 * zoomfactor, 0), New Point3D(30 * i * zoomfactor, 300 * zoomfactor, 0))

                ' Axi(1) = New Segment3D(New Point3D(r, 10, 0), New Point3D(y1, 610, 0))

                drawline(Axi(1).Pt1, Axi(1).Pt2, Color.FromArgb(50, 0, 255, 0))
                drawline(Axi(2).Pt1, Axi(2).Pt2, Color.FromArgb(50, 0, 255, 0))

            Next
        Else
            For i As Integer = 1 To 10

                Dim Axi(2) As Segment3D
                Axi(1) = New Segment3D(New Point3D(30, 30 * i, 0), New Point3D(300, 30 * i, 0))
                Axi(2) = New Segment3D(New Point3D(30 * i, 30, 0), New Point3D(30 * i, 300, 0))

                ' Axi(1) = New Segment3D(New Point3D(r, 10, 0), New Point3D(y1, 610, 0))

                drawline(Axi(1).Pt1, Axi(1).Pt2, Color.FromArgb(50, 0, 255, 0))
                drawline(Axi(2).Pt1, Axi(2).Pt2, Color.FromArgb(50, 0, 255, 0))

            Next
        End If

    End Sub
    Sub sd()

        Dim ax As Integer = 1 * zoomfactor
        Dim ax1 As Integer = 1 * zoomfactor
        Dim ax2 As Integer = 100 * zoomfactor

        Axis(0) = New Segment3D(New Point3D(ax1, ax, 0), New Point3D(ax2, ax, 0))
        Axis(1) = New Segment3D(New Point3D(ax, ax1, 0), New Point3D(ax, ax2, 0))
        Axis(2) = New Segment3D(New Point3D(ax, ax, ax1), New Point3D(ax, ax, ax2))

        Dim axw As Integer = 10 * zoomfactor
        Dim axw1 As Integer = 50 * zoomfactor

        pt3dC(0) = New Point3D(axw, axw, axw)
        pt3dC(1) = New Point3D(axw1, axw, axw)
        pt3dC(2) = New Point3D(axw1, axw1, axw)
        pt3dC(3) = New Point3D(axw, axw1, axw)
        pt3dC(4) = New Point3D(axw, axw, axw1)
        pt3dC(5) = New Point3D(axw1, axw, axw1)
        pt3dC(6) = New Point3D(axw1, axw1, axw1)
        pt3dC(7) = New Point3D(axw, axw1, axw1)
    End Sub
    Private Sub PictureBox1_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles PictureBox1.Resize
        View2D.Centre.X = Me.Width / 2
        View2D.Centre.Y = Me.Height / 2

    End Sub
#Region " autorotation"
    Private Timer_dThetaX0, Timer_dThetaY0 As Single

    Public Sub StartAutorotation(ByVal dThetaX0 As Single, ByVal dThetay00 As Single)

        Timer_dThetaX0 = dThetaX0 + 1
        Timer_dThetaY0 = dThetay00 + 1
        Timer1.Enabled = True
    End Sub

    Private Sub Timer1_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Timer1.Tick
        Cam3D.RotateCamera(Timer_dThetaX0, Timer_dThetaY0)
        ' Cam3D.RotateCamera(Timer_dThetaX0, 0, 0)
        Me.Invalidate()
    End Sub
    Public Sub StopAutorotation()
        Timer1.Enabled = False
    End Sub

    Dim dx, dy As Single

    Private Sub picturebox1_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseUp
        If (Math.Abs(dx) >= 2) Or (Math.Abs(dy) >= 2) Then
            If dx > 2 Then dx = 3 : If dx <= -2 Then dx = -2
            If dy > 2 Then dy = 3 : If dy <= -2 Then dy = -2
            If Abs(dx) <= 1 Then dx = 0
            If Abs(dy) <= 1 Then dy = 0
            StartAutorotation(-dx * 0.8, -dy * 0.8)
            dx = 0
            dy = 0
        End If

    End Sub
#End Region
    Private Sub PictureBox1_MouseWheel(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Me.MouseWheel
        If e.Delta > 0 Then
            If zoomfactor >= 20 Then zoomfactor = 20 : Exit Sub
            zoomfactor = zoomfactor + 0.1

            Me.Invalidate()

        ElseIf e.Delta < 0 Then
            If zoomfactor <= 0.08 Then zoomfactor = 0.08 : Exit Sub
            zoomfactor = zoomfactor - 0.1
            Me.Invalidate()

        End If
    End Sub

    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged
        Cam3D.UpdateCamera(100, 0, 0, 0)
        LastMousePosition.X = 0
        LastMousePosition.Y = 0
        Me.Invalidate()
    End Sub

    Private Sub RadioButton2_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton2.CheckedChanged
        ' For i As Integer = 0 To 300
        Cam3D.UpdateCamera(100, 180, 180, 0)
        LastMousePosition.X = 180
        LastMousePosition.Y = 180

        Me.Invalidate()
        ' Next
    End Sub

    Private Sub RadioButton3_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton3.CheckedChanged
        Cam3D.UpdateCamera(100, 90, 90, 0)
        LastMousePosition.X = 90
        LastMousePosition.Y = 90

        Me.Invalidate()
    End Sub

    Private Sub RadioButton4_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton4.CheckedChanged
        Cam3D.UpdateCamera(100, 0, 0, 90)
        LastMousePosition.X = 0
        LastMousePosition.Y = 90

        Me.Invalidate()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Dim openfile = New OpenFileDialog()
        openfile.Filter = "Text (*.txt)|*.txt"
        If (openfile.ShowDialog() = System.Windows.Forms.DialogResult.OK) Then
            Dim myfile As String = openfile.FileName
            Dim allLines As String() = File.ReadAllLines(myfile)
            For Each line As String In allLines
                ListBox2.Items.Add(line)
            Next
        End If
    End Sub

#End Region

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        OrbitRadius = 200
        OrbitSpeed = 0.001
        Rw = 1000
        Viewtheta = 0
        Viewfi = 0
        g000 = PictureBox1.CreateGraphics
        Focus3D = New Point3D(0, 0, 0)
        View2D.Centre.X = 80
        View2D.Centre.Y = 10

        DataGridView1.Rows.Add({"100", " 100", " 0", " 200", " 200", " 500"})
        DataGridView1.Rows.Add({"100 ", "300", " 0", " 200", " 200", " 500"})
        DataGridView1.Rows.Add({"300 ", "100", " 0 ", "200", " 200", " 500"})
        DataGridView1.Rows.Add({"300", " 300", " 0", " 200", " 200", " 500"})
        DataGridView1.Rows.Add({"100", " 100", " 0", " 100", " 300", " 0"})
        DataGridView1.Rows.Add({"100", " 300", " 0", " 300", " 300", " 0"})
        DataGridView1.Rows.Add({"300", " 300", " 0", " 300", " 100", " 0"})
        DataGridView1.Rows.Add({"100", " 100", " 0", " 300", " 100", " 0"})
    End Sub

End Class