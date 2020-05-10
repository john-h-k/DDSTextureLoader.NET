# DDSTextureLoader.NET

 A DDS texture loader for .NET programs

## Texture Creation

First, a texture descriptor `DdsTextureDescription` must be created from the DDS file. To do this, you call  `DdsTextureLoader.CreateDdsTexture`. 

 ```cs
public static DdsTextureDescription CreateDdsTexture(
            string filename,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None
);

public static DdsTextureDescription CreateDdsTexture(
            Stream stream,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None
);

public static DdsTextureDescription CreateDdsTexture(
            Memory<byte> ddsData,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None
);
```

After creation, you can inspect the read-only struct `DdsTextureDescription`.
You then need to schedule it for upload using

```cs
public static void RecordTextureUpload(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            in DdsTextureDescription textureDescription,
            out ID3D12Resource* textureBuffer,
            out ID3D12Resource* textureBufferUploadHeap,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE
);
```

You must then execute the command list, and once it is done, the texture is present in `textureBuffer`.
