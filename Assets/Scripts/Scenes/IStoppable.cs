using System.Collections;
using UnityEngine;

public interface IStoppable
{
	void Regist_MovableStopper();
	IEnumerator Start_FrameDelay(int frame);
}