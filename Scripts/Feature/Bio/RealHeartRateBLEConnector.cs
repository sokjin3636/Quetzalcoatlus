using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using UnityEngine.Events;
using System.Collections.Generic;

public class RealHeartRateBLEConnector : MonoBehaviour
{
    public RealHeartRateManager hrManager;
    public Text statusText;

    [Header("--- 연결 성공 콜백 이벤트 ---")]
    public UnityEvent OnConnectionReady;

    private Dictionary<string, string> foundDevices = new Dictionary<string, string>();
    private string _targetAddress;
    private string ServiceUUID = "180D";
    private string CharacteristicUUID = "2A37";

    void Start()
    {
        RequestBluetoothPermissions();
    }

    private void RequestBluetoothPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string[] permissions = {
            "android.permission.BLUETOOTH_SCAN",
            "android.permission.BLUETOOTH_CONNECT",
            "android.permission.ACCESS_FINE_LOCATION"
        };

        foreach (string permission in permissions)
        {
            if (!Permission.HasUserAuthorizedPermission(permission))
            {
                UpdateStatus("권한 요청 중...");
                Permission.RequestUserPermissions(permissions);
                break;
            }
        }
#endif
    }

    // BLE 기기 스캔 시작
    public void StartScanButtonPressed()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            UpdateStatus("권한 거부됨. 설정 확인 필요.");
            RequestBluetoothPermissions();
            return;
        }
#endif

        foundDevices.Clear();
        _targetAddress = "";
        UpdateStatus("모든 기기 검색 중...");

        BluetoothLEHardwareInterface.Initialize(true, false, () => {
            BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) => {
                if (!foundDevices.ContainsKey(address))
                {
                    string displayName = string.IsNullOrEmpty(name) ? "Unknown Device" : name;
                    foundDevices.Add(address, displayName);
                    RefreshDeviceListUI();
                }
            });
        }, (error) => {
            UpdateStatus("BLE 초기화 실패: " + error);
        });
    }

    // 스캔된 기기 목록 UI 갱신 및 식별자 매칭
    void RefreshDeviceListUI()
    {
        string listText = "--- Found Devices ---\n";
        foreach (var device in foundDevices)
        {
            listText += $"{device.Value} [{device.Key}]\n";

            if (device.Value.Contains("Coospo") ||
                device.Value.Contains("Heart") ||
                device.Value.Contains("H6") ||
                device.Value.Contains("808S"))
            {
                _targetAddress = device.Key;
                listText += " <--- TARGET FOUND!\n";
            }
        }
        UpdateStatus(listText);
    }

    // 타겟 기기 연결 요청
    public void ConnectButtonPressed()
    {
        if (string.IsNullOrEmpty(_targetAddress))
        {
            UpdateStatus("No target device found yet!");
            return;
        }

        UpdateStatus("Connecting to " + _targetAddress);
        BluetoothLEHardwareInterface.StopScan();

        BluetoothLEHardwareInterface.ConnectToPeripheral(_targetAddress, (address) => {
            UpdateStatus("Connected! Discovering Services...");
        }, null, (address, sID, cID) => {
            if (cID.ToUpper().Contains(CharacteristicUUID))
            {
                UpdateStatus("Device Ready. Reading Data...");
                SubscribeToData();

                // 데이터 구독 완료 후 지정된 이벤트 호출
                if (OnConnectionReady != null)
                {
                    OnConnectionReady.Invoke();
                }
            }
        }, (address) => {
            UpdateStatus("Disconnected.");
        });
    }

    void SubscribeToData()
    {
        BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(_targetAddress, ServiceUUID, CharacteristicUUID, null, (address, characteristic, data) => {
            if (data != null && data.Length > 0)
            {
                // 수집 상태에 따른 데이터 파싱 처리
                hrManager.ParseHeartRateData(data);
            }
        });
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log(message);
    }
}