using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerTestConfigurationUtils
{
public class TestConfigurationException : Exception
{
    public string message;

    public TestConfigurationException(string _message)
    {
        message = _message;
    }
}
}
