using BehaviorDesigner.Runtime;
using System;
using UnityEngine;

public class AOTLinker : MonoBehaviour
{
    public void Linker()
    {
        BehaviorManager.BehaviorTree tree = new BehaviorManager.BehaviorTree();
        BehaviorManager.BehaviorTree.ConditionalReevaluate reevaluate = new BehaviorManager.BehaviorTree.ConditionalReevaluate();
        BehaviorManager.TaskAddData data = new BehaviorManager.TaskAddData();
        BehaviorManager.TaskAddData.OverrideFieldValue value2 = new BehaviorManager.TaskAddData.OverrideFieldValue();
    }
}

