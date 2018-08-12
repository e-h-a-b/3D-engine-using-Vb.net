Imports AdvanceMath
Imports Geometry3D
Public Class Camera3D
    'POSITION OF CAMERA IN WORLD SPACE
    '----------------------------------------------------
    'R=Orbit radius camera will rotate
    'theta=Angle of projection of camera Position vector in XY plane, with World's X axis (xpitch)
    'Fi=Angle of CameraPosition vector with world's Z axis
    Public Caption As String                   '   Camera1, Director's Chair, Birds-eye View, etc. (Optional)
    Public Description As String               '   A Caption should always have a Description.     (Optional)
    'Public ProjectionMode As eProjection
    Public R As Single
    Public Theta, Fi As Single  'angles in degree
    ' Public Yaw, Pitch, Roll As Single  'angles in degree
    Public bPerspective As Boolean
    'used with perspective view only
    Public Nearplane As Single
    Public FarPlane As Single
    Public FOV As Single                       '   Field Of View (FOV). "90 degree FOV" = "1x Zoom". If you update FOV, don't forget to update Zoom.
    Public Zoom As Single                      '   (The Zoom value is calculated from the FOV. Normally you define one of them, then calculate the other.)
    Public ClipFar As Single                   '   Don't draw Vertices further away than this value. Any value higher than 0.
    Public ClipNear As Single                  '   Don't draw Vertices that are this close to us (or behind us). Typically 0, but can be higher.
    Private Mat_View As New Matrix4x4
    'HEAD OF CAMERA (UP Vector)
    '------------------------------------------------------
    'CamUPPos: represents Point in camera used to specify up direction of camera
    'UPVector: up Vector of camrea is vector Joining PV of camUPPos with PV of camCentrePOS
    'Line of Sight/Look at Vector: Vector joining CamCentrePos and Focus(targetPos)
    Public camCentrePos, Focus, CamUpPos As Point3D
    Public TempCamPos, TempUppt, Tempfocus As Point3D

    'LOCAL AXIS OF CAMERA SPACE
    '-------------------------------------------------------
    'Z axis: or N Vector vector joining Focus and CamcentrePos 
    'Y axis: or UpVector Projection of UPvector in Projection Plane
    'X axis: Or Right vector: crossProduct(Y axis, Z axis)
    Public LocalX, LocalY, LocalZ As Vector3D
    Public Event camera_Updated(ByVal Sender As Camera3D)

    'Set defaultCamera
    Public Sub New()
        R = 1000
        Theta = 0
        Fi = 0
        camCentrePos = New Point3D(0, 0, 400)
        Focus = New Point3D(0, 0, 0)
        CamUpPos = New Point3D(0, 1, 400)
        bPerspective = False
        ZoomFactor = 1
        UpdateCamera(R, Theta, Fi)
        ' Mat_View = New Matrix4x4
    End Sub
    'LookFrom : CameraCentre position in worldSpace
    'Lookat   : Focus in world Space
    'CameraTop: The position of Head of camera, to specify the upvector =Cameratop-lookfrom
    Public Sub New(ByVal LookFrom As Point3D, ByVal Lookat As Point3D, ByVal CameraTop As Point3D)
        R = 100
        Theta = 0
        Fi = 90
        camCentrePos = LookFrom
        Focus = Lookat
        CamUpPos = CameraTop
        get_ThetaFi_OfPoint3D(camCentrePos, R, Theta, Fi)
        bPerspective = False
        ZoomFactor = 1
        'Update localX(right), localY(up), localZ(n vector) ie. local Camera Space
        UpdateCamera(R, Theta, Fi)
    End Sub
    'Toggles Perspctive/Parallel view
    Public Property Perspective As Boolean
        Get
            Return bPerspective
        End Get
        Set(ByVal value As Boolean)
            If bPerspective <> value Then
                bPerspective = value
                RaiseEvent camera_Updated(Me)
            End If
        End Set
    End Property
    'Set field of View of Camera
    'Field Of View (FOV). "90 degree FOV" = "1x Zoom". If you update FOV, don't forget to update Zoom.
    Public Property FOView As Single
        Get
            Return FOV
        End Get
        Set(ByVal value As Single)
            If FOV <> value Then
                FOV = value
                RaiseEvent camera_Updated(Me)
            End If
        End Set
    End Property
    'Zoom factor
    '(The Zoom value is calculated from the FOV. Normally you define one of them, then calculate the other.)
    Public Property ZoomFactor As Single
        Get
            Return Zoom
        End Get
        Set(ByVal value As Single)
            If Zoom <> value Then
                Zoom = value
                RaiseEvent camera_Updated(Me)
            End If
        End Set
    End Property
    'Rotate camera by specified angle
    Public Sub RotateCamera(ByVal dTheta As Single, ByVal dFi As Single)
        UpdateCamera(R, Theta + dTheta, Fi + dFi)
    End Sub
    'Rotate camera by specified angle
    Public Sub RotateCamera(ByVal dThetaX As Single, ByVal dThetaY As Single, ByVal dThetaZ As Single)
        'UpdateCamera(R, Theta + dThetaX, Fi + dThetaZ)
        Dim m As Matrix4x4
        m = Matrix4x4.MatrixRotateZ(dThetaZ) * Matrix4x4.MatrixRotateX(dThetaX) * Matrix4x4.MatrixRotateY(dThetaY)

        'Rotate camera by theta ,fi by transforming camPos and CamUppos accordingly, Save these in temp  Variables 
        TempCamPos = applytransform(TempCamPos, m)
        TempUppt = applytransform(TempUppt, m)
        get_ThetaFi_OfPoint3D(TempCamPos, R, Me.Theta, Me.Fi)
        ' Me.Theta = Me.Theta + dThetaX
        'Me.Fi = Me.Fi + dThetaZ
        'get UPvector
        LocalY = VectorOf_2Points3D(TempCamPos, TempUppt)
        'get Line of view Vector
        LocalZ = -VectorOf_2Points3D(Focus, TempCamPos).Normalize()
        'get Right Vector
        LocalX = (LocalZ * LocalY).Normalize
        'ReEvaluate upVector,sothat it lies in plane of projection (in case upvector is not perpendicular to look at vector
        LocalY = LocalX * LocalZ 'The cross-product give a normalized vector, because both input vectors are normalized,  '  then we dont need to normalize.

        RaiseEvent camera_Updated(Me)
    End Sub

    'Update local Coordinate System in cameraSpace(It is left handed system)
    ' The local Z is the lookat vector (from the camera center to the lookat point) and is "into the screen".
    ' The local Y will be toward the up of the screen. 
    '                     If you maintain your camera up vector perpendicular to the camera lookat vector, then the up vector is also the local Y. 
    ' The local X will be normal to the two other axes (it is their cross product after their normalization).
    ' Rotating left-right is a matter of rotating the lookat vector about the local Y axis (yaw). 
    ' Rotating up-down is a matter of rotating the up vector and the lookat vector about the local X axis (pitch). 
    ' Tilting the head left-right is a matter of rotating up vector about the local Z axis (ROLL).
    ' Each time you change the lookat vector or the up vector, you need to recompute the local axes.
    Public Sub UpdateCamera(ByVal OrbitRadius As Single, ByVal sTheta As Single, ByVal sFi As Single, Optional ByVal dThetaY As Single = 0)
        'update Camera parameters
        Me.R = OrbitRadius
        Me.Theta = sTheta
        Me.Fi = sFi
        Dim M As Matrix4x4
        'Create 4x4matric to rotate camera by theta(about z axis) and fi(about x axis)
        M = Matrix4x4.MatrixRotateZ(Theta) * Matrix4x4.MatrixRotateX(Fi) * Matrix4x4.MatrixRotateY(dThetaY)

        'Rotate camera by theta ,fi by transforming camPos and CamUppos accordingly, Save these in temp  Variables 
        TempCamPos = applytransform(camCentrePos, M)
        TempUppt = applytransform(CamUpPos, M)

        'get UPvector
        LocalY = VectorOf_2Points3D(TempCamPos, TempUppt)
        'get Line of view Vector
        LocalZ = -VectorOf_2Points3D(Focus, TempCamPos).Normalize()
        'get Right Vector
        LocalX = (LocalZ * LocalY).Normalize
        'ReEvaluate upVector,sothat it lies in plane of projection (in case upvector is not perpendicular to look at vector
        LocalY = LocalX * LocalZ 'The cross-product give a normalized vector, because both input vectors are normalized,  '  then we dont need to normalize.

        RaiseEvent camera_Updated(Me)
        'The following code is for using 4x4matrix to project
        'LocalY = (New Vector3D(0, 1, 0))
        'LocalY = Matrix4x4.applytransform(LocalY, M)
        'Dim C, F As Vector3D
        'C = New Vector3D(TempCamPos.x, TempCamPos.y, TempCamPos.z)
        'F = New Vector3D(Tempfocus.x, Tempfocus.y, Tempfocus.z)
        'Mat_View = Matrix4x4.matrix_Project3D(0, C, F, LocalY)
    End Sub

    'project point in XY plane in cameraSpace
    Public Function Project3Dto2D(ByVal Pt As Point3D) As Point3D
        'rv=Return Value
        Dim rv As New Point3D(0, 0, 0)

        'find the vector joining pt and focus, treating focus as origin
        pt.x = (pt.x - Focus.x)
        pt.y = (pt.y - Focus.y)
        pt.z = (pt.z - Focus.z)

        'get Projection length of PV of point along orthogonal unit vectors localX ,localY and localZ by taking dot Product
        rv.x = pt.x * LocalX.x + pt.y * LocalX.y + pt.z * LocalX.z
        rv.y = pt.x * LocalY.x + pt.y * LocalY.y + pt.z * LocalY.z
        rv.z = pt.x * LocalZ.x + pt.y * LocalZ.y + pt.z * LocalZ.z

        If bPerspective Then
            rv.x = rv.x / (1 + rv.z / R)
            rv.y = rv.y / (1 + rv.z / R)
        End If

        Return rv
    End Function

    Private Function ConvertFOVtoZoom(ByVal FOV As Single) As Single
        ' Given a Field Of View in degree  calculate the Zoom.
        ConvertFOVtoZoom = 1 / Tan_D((FOV) / 2)
    End Function

    Private Function ConvertZoomtoFOV(ByVal Zoom As Single) As Single
        ' Given a Zoom value, calculate the 'Field Of View' in degree
        ConvertZoomtoFOV = 2 * Atan_D(1 / Zoom)
    End Function
End Class
