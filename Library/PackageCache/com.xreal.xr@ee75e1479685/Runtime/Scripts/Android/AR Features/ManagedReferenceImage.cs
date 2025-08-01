#if XR_ARFOUNDATION
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.XR.XREAL
{
    [StructLayout(LayoutKind.Sequential)]
    struct ManagedReferenceImage : IDisposable
    {
        public ManagedReferenceImage(XRReferenceImage referenceImage)
        {
            guid = referenceImage.guid;
            textureGuid = referenceImage.textureGuid;
            size = referenceImage.specifySize ? referenceImage.size : Vector2.zero;
            name = GCHandle.ToIntPtr(GCHandle.Alloc(referenceImage.name));
            texture = GCHandle.ToIntPtr(GCHandle.Alloc(referenceImage.texture));
        }

        public unsafe XRReferenceImage ToReferenceImage()
        {
            Vector2? maybeSize;
            if (size.x > 0)
            {
                maybeSize = size;
            }
            else
            {
                maybeSize = null;
            }

            return new XRReferenceImage(
                AsSerializedGuid(guid),
                AsSerializedGuid(textureGuid),
                maybeSize,
                ResolveGCHandle<string>(name),
                ResolveGCHandle<Texture2D>(texture));
        }

        unsafe SerializableGuid AsSerializedGuid(Guid guid)
        {
            TrackableId trackableId;
            *(Guid*)&trackableId = guid;
            return new SerializableGuid(trackableId.subId1, trackableId.subId2);
        }

        public void Dispose()
        {
            GCHandle.FromIntPtr(texture).Free();
            texture = IntPtr.Zero;

            GCHandle.FromIntPtr(name).Free();
            name = IntPtr.Zero;
        }

        static T ResolveGCHandle<T>(IntPtr ptr) where T : class => (ptr == IntPtr.Zero) ? null : GCHandle.FromIntPtr(ptr).Target as T;

        public Guid guid;
        public Guid textureGuid;
        public Vector2 size;
        public IntPtr name;
        public IntPtr texture;
    }

    static class ManagedReferenceImageExtensions
    {
        public static NativeArray<ManagedReferenceImage> ToNativeArray(this XRReferenceImageLibrary library, Allocator allocator)
        {
            var managedReferenceImages = new NativeArray<ManagedReferenceImage>(library.count, allocator);
            for (var i = 0; i < library.count; ++i)
            {
                managedReferenceImages[i] = new ManagedReferenceImage(library[i]);
            }

            return managedReferenceImages;
        }
    }
}
#endif
