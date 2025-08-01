﻿using System;
using UnityEngine;

namespace Unity.XR.XREAL
{
    public interface IEncoderBase
    {

    }
    /// <summary> Interface for encoder. </summary>
    public interface IEncoder : IEncoderBase
    {
        /// <summary> Configurations the given parameter. </summary>
        /// <param name="param"> The parameter.</param>
        void Config(CameraParameters param);

        /// <summary> Commits. </summary>
        /// <param name="rt">        The right.</param>
        /// <param name="timestamp"> The timestamp.</param>
        void Commit(RenderTexture rt, UInt64 timestamp);

        /// <summary> Starts this object. </summary>
        void Start();

        /// <summary> Stops this object. </summary>
        void Stop();

        /// <summary> Releases this object. </summary>
        void Release();
    }
}
