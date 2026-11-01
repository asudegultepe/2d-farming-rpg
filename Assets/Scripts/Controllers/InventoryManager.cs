using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] GameObject statusPanel;
    [SerializeField] GameObject toolbarPanel;
    [SerializeField] GameObject dayTime;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ClosePanel();
            toolbarPanel.SetActive(!toolbarPanel.activeInHierarchy);
        }
    }

    public void ClosePanel()
    {
        panel.SetActive(!panel.activeInHierarchy);
        statusPanel.SetActive(!statusPanel.activeInHierarchy);
        dayTime.SetActive(!dayTime.activeInHierarchy);
    }
}
