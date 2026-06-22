using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using System.Collections.Generic;

public class HeartRateBLEConnector : MonoBehaviour
{
    public HeartRateManager hrManager;
    public Text statusText;

    private Dictionary<string, string> foundDevices = new Dictionary<string, string>();
    private string _targetAddress;
    private string ServiceUUID = "180D";
    private string CharacteristicUUID = "2A37";

    void Start()
    {
        // 시작 시 블루투스 및 위치 권한 요청
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
                UpdateStatus("Requesting System Permissions...");
                Permission.RequestUserPermissions(permissions);
                break;
            }
        }
#endif
    }

    // BLE 스캔 시작
    public void StartScanButtonPressed()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            UpdateStatus("Permission Denied. Check Quest Settings.");
            RequestBluetoothPermissions();
            return;
        }

        foundDevices.Clear();
        UpdateStatus("Scanning all devices...");

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
            UpdateStatus("BLE Init Error: " + error);
        });
    }

    void RefreshDeviceListUI()
    {
        string listText = "--- Found Devices ---\n";
        foreach (var device in foundDevices)
        {
            listText += $"{device.Value} [{device.Key}]\n";

            // 심박계 식별자 필터링
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

    // 대상 기기 연결
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
                hrManager.ParseHeartRateData(data);
                UpdateStatus($"BPM: {hrManager.currentBPM} | R-R: {hrManager.currentRR:F0}ms | RMSSD: {hrManager.currentRMSSD:F1}ms");
            }
        });
    }

    void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log(message);
    }
}