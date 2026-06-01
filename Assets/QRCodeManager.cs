using Meta.XR.MRUtilityKit;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

public class QRCodeManager : MonoBehaviour
{
    public GameObject qrCodePrefab;
    public GameObject cube;

    [SerializeField] private OVRInput.Button tapeActionButton;

    public GameObject positionBalls;
    public GameObject PositionBallsParents;
    public GameObject RemotePosition;
    public GameObject CoolerModel;

    Quaternion MainCoolerTargetRotation;
    Vector3 MainCoolerMidPoint;
    Vector3 QRCodePosition;
    [Header("Line Renderers")]
    [SerializeField] private LineRenderer directionLine;

    [SerializeField] private LineRenderer perpendicularLine;

    void Start()
    {
        MRUK.Instance.SceneSettings.TrackableAdded.AddListener(OnTrackableAdded);
    }

    void Update()
    {
        if(PositionBallsParents.transform.childCount <= 2)
        {
            if (OVRInput.GetDown(tapeActionButton))
            {
                SpawnCalibrationPoint();
            }

        }
    }

    private void SpawnCalibrationPoint()
    {
        GameObject ball = Instantiate(
            positionBalls,
            RemotePosition.transform.position,
            Quaternion.identity
        );

        ball.transform.SetParent(PositionBallsParents.transform);

        if (PositionBallsParents.transform.childCount > 1)
        {
            AlignCoolerUsingPoints();
        }
    }

    private void AlignCoolerUsingPoints()
    {
        Transform point1 =
            PositionBallsParents.transform.GetChild(0);

        Transform point2 =
            PositionBallsParents.transform.GetChild(1);

        MainCoolerMidPoint =
            (point1.position + point2.position) * 0.5f;

        Vector3 direction =
            point2.position - point1.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        Vector3 perpendicularDirection =
            Vector3.Cross(direction, Vector3.up);

        perpendicularDirection.Normalize();

        float lineLength = 1.5f;

        directionLine.positionCount = 2;

        directionLine.SetPosition(
            0,
            point1.position
        );

        directionLine.SetPosition(
            1,
            point1.position + direction * lineLength
        );

        perpendicularLine.positionCount = 2;

        perpendicularLine.SetPosition(
            0,
            point1.position
        );

        perpendicularLine.SetPosition(
            1,
            point1.position + perpendicularDirection * lineLength
        );

        MainCoolerTargetRotation =
            Quaternion.LookRotation(
                perpendicularDirection,
                Vector3.up
            );
            cube.transform.rotation =
        MainCoolerTargetRotation;

        //cube.transform.position = new Vector3(
        //    cube.transform.position.x,
        //    cube.transform.position.y,
        //    MainCoolerMidPoint.z
        //);
        Transform cubeRef =
            cube.transform.GetChild(0);
        Vector3 positionOffset =
            QRCodePosition - cubeRef.position;

        cube.transform.position += positionOffset;

        //REMOVE THIS LINE
        //cube.transform.position = MainCoolerMidPoint;
        CoolerModel.SetActive(true);
    }

    private void OnTrackableAdded(MRUKTrackable qrCode)
    {
        if (qrCode.TrackableType != OVRAnchor.TrackableType.QRCode)
            return;

        Vector3 targetPosition = qrCode.transform.position;
        QRCodePosition = targetPosition;

        Quaternion qrRotation =
            Quaternion.LookRotation(
                -qrCode.transform.forward,
                qrCode.transform.up
            );

        GameObject qrCodeObject =
            Instantiate(
                qrCodePrefab,
                targetPosition,
                qrRotation
            );

        float width = qrCode.PlaneRect.Value.width;
        float height = qrCode.PlaneRect.Value.height;

        Vector3 targetScale =
            new Vector3(width, height, 0);

        qrCodeObject.transform.localScale = targetScale;

        

        

        
    }
}