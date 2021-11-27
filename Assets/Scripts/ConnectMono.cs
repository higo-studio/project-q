using UnityEngine;
using UnityEngine.UI;
using Unity.NetCode;
using Unity.Networking.Transport;
using System;
using Unity.Entities;
using TMPro;


public class ConnectMono : MonoBehaviour
{
    public TMP_InputField input;
    public Button connectBtn;

    private void OnEnable()
    {
        connectBtn.onClick.AddListener(Connect);
    }

    private void OnDisable()
    {
        connectBtn.onClick.RemoveListener(Connect);
    }

    public void Connect()
    {
        if (string.IsNullOrEmpty(input.text)) return;
        var arugs = input.text.Split(':');
        if (arugs.Length < 2) return;
        var address = arugs[0];
        var port = Convert.ToUInt32(arugs[1]);
        if (NetworkEndPoint.TryParse(address, (ushort)port, out var ep))
        {
            foreach (var world in World.All)
            {
                var isClientWorld = world.GetExistingSystem<ClientSimulationSystemGroup>() != null;
                if (isClientWorld)
                {
                    var network = world.GetExistingSystem<NetworkStreamReceiveSystem>();
                    network.Connect(ep);
                    gameObject.SetActive(false);
                }
            }
        }
    }
}