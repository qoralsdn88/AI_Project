using System.Collections.Generic;
using UnityEngine;

public class MovableStopper : MonoBehaviour
{
	private static MovableStopper instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public static MovableStopper Instance { get => instance; }

    ///////////////////////////////////////////////////////////////////////////

    private List<IStoppable> stoppers = new List<IStoppable>();

    public void Regist(IStoppable stopper)
    {
        stoppers.Add(stopper);
    }

    public void Start_Delay(int frame)
    {
        if (frame < 1)
            return;

        stoppers.ForEach(stopper =>
        {
            StartCoroutine(stopper.Start_FrameDelay(frame));
        });
    }
}