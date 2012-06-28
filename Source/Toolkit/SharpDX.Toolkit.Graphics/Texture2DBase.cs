// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace SharpDX.Toolkit.Graphics
{
    /// <summary>
    /// Abstract class frontend to <see cref="SharpDX.Direct3D11.Texture2D"/>.
    /// </summary>
    public abstract class Texture2DBase : Texture<Direct3D11.Texture2D>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Texture2DBase" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="dataRectangles">A variable-length parameters list containing data rectangles.</param>
        /// <msdn-id>ff476521</msdn-id>
        ///   <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>
        ///   <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>
        protected internal Texture2DBase(Texture2DDescription description, params DataRectangle[] dataRectangles)
            : this(GraphicsDevice.Current, description, dataRectangles)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture2DBase" /> class.
        /// </summary>
        /// <param name="device">The device local.</param>
        /// <param name="description">The description.</param>
        /// <param name="dataRectangles">A variable-length parameters list containing data rectangles.</param>
        /// <msdn-id>ff476521</msdn-id>	
        /// <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>	
        /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>	
        protected internal Texture2DBase(GraphicsDevice device, Texture2DDescription description, params DataRectangle[] dataRectangles)
        {
            Description = description;
            // Precalculates the stride
            StrideInBytes = Description.Width * ((PixelFormat) Description.Format).SizeInBytes;
            Initialize(device, new Direct3D11.Texture2D(device, description, dataRectangles));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture2DBase" /> class.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <msdn-id>ff476521</msdn-id>	
        /// <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>	
        /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>	
        protected internal Texture2DBase(Direct3D11.Texture2D texture)
            : this(GraphicsDevice.Current, texture)
        {
        }

        /// <summary>
        /// Specialised constructor for use only by derived classes.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="texture">The texture.</param>
        /// <msdn-id>ff476521</msdn-id>	
        /// <unmanaged>HRESULT ID3D11Device::CreateTexture2D([In] const D3D11_TEXTURE2D_DESC* pDesc,[In, Buffer, Optional] const D3D11_SUBRESOURCE_DATA* pInitialData,[Out, Fast] ID3D11Texture2D** ppTexture2D)</unmanaged>	
        /// <unmanaged-short>ID3D11Device::CreateTexture2D</unmanaged-short>	
        protected internal Texture2DBase(GraphicsDevice device, Direct3D11.Texture2D texture)
        {
            Description = texture.Description;
            StrideInBytes = Description.Width * ((PixelFormat)Description.Format).SizeInBytes;
            Initialize(device, texture);
        }

        /// <summary>
        /// The description of this <see cref="Texture2DBase"/>.
        /// </summary>
        public readonly Texture2DDescription Description;

        /// <summary>
        /// The stride - number of bytes per row - in bytes.
        /// </summary>
        public readonly int StrideInBytes;

        /// <summary>
        /// Makes a copy of this texture.
        /// </summary>
        /// <remarks>
        /// This method doesn't copy the content of the texture.
        /// </remarks>
        /// <returns>
        /// A copy of this texture.
        /// </returns>
        public abstract Texture2DBase Clone();

        /// <summary>
        /// Makes a copy of this texture.
        /// </summary>
        /// <remarks>
        /// This method doesn't copy the content of the texture.
        /// </remarks>
        /// <returns>
        /// A copy of this texture.
        /// </returns>
        public T Clone<T>() where T : Texture2DBase
        {
            return (T)Clone();
        }

        /// <summary>
        /// Return an equivalent staging texture CPU read-writable from this instance.
        /// </summary>
        /// <returns></returns>
        public Texture2D ToStaging()
        {
            var stagingDesc = Description;
            stagingDesc.BindFlags = BindFlags.None;
            stagingDesc.CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write;
            stagingDesc.Usage = ResourceUsage.Staging;
            stagingDesc.OptionFlags = ResourceOptionFlags.None;
            return new Texture2D(this.GraphicsDevice, stagingDesc);            
        }

        public override ShaderResourceView GetShaderResourceView(ViewSlice viewSlice, int arrayIndex, int mipIndex)
        {
            if ((Description.BindFlags & BindFlags.ShaderResource) == 0)
                return null;

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(viewSlice, ref arrayIndex, ref mipIndex, out arrayCount, out mipCount);

            var srvIndex = GetViewIndex(viewSlice, arrayIndex, mipIndex);
            var srv = ShaderResourceViews[srvIndex];

            // Creates the shader resource view
            if (srv == null)
            {
                // Create the view
                var srvDescription = new ShaderResourceViewDescription() { Format = Description.Format };

                // Initialize for texture arrays or texture cube
                if (Description.ArraySize > 1)
                {
                    // If texture cube
                    if ((Description.OptionFlags & ResourceOptionFlags.TextureCube) != 0)
                    {
                        srvDescription.Dimension = ShaderResourceViewDimension.TextureCube;
                        srvDescription.TextureCube.MipLevels = mipCount;
                        srvDescription.TextureCube.MostDetailedMip = mipIndex;
                    }
                    else
                    {
                        // Else regular Texture2D
                        srvDescription.Dimension = Description.SampleDescription.Count > 1 ? ShaderResourceViewDimension.Texture2DMultisampledArray : ShaderResourceViewDimension.Texture2DArray;

                        // Multisample?
                        if (Description.SampleDescription.Count > 1)
                        {
                            srvDescription.Texture2DMSArray.ArraySize = arrayCount;
                            srvDescription.Texture2DMSArray.FirstArraySlice = arrayIndex;
                        }
                        else
                        {
                            srvDescription.Texture2DArray.ArraySize = arrayCount;
                            srvDescription.Texture2DArray.FirstArraySlice = arrayIndex;
                            srvDescription.Texture2DArray.MipLevels = mipCount;
                            srvDescription.Texture2DArray.MostDetailedMip = mipIndex;
                        }
                    }
                }
                else
                {
                    srvDescription.Dimension = Description.SampleDescription.Count > 1 ? ShaderResourceViewDimension.Texture2DMultisampled : ShaderResourceViewDimension.Texture2D;
                    if (Description.SampleDescription.Count <= 1)
                    {
                        srvDescription.Texture2D.MipLevels = mipCount;
                        srvDescription.Texture2D.MostDetailedMip = mipIndex;
                    }
                }

                srv = new ShaderResourceView(this.GraphicsDevice, this.Resource, srvDescription);
                ShaderResourceViews[srvIndex] = ToDispose(srv);
            }

            return srv;
        }

        public override UnorderedAccessView GetUnorderedAccessView(int arrayIndex, int mipIndex)
        {
            if ((Description.BindFlags & BindFlags.UnorderedAccess) == 0)
                return null;

            int arrayCount;
            int mipCount;
            GetViewSliceBounds(ViewSlice.Single, ref arrayIndex, ref mipIndex, out arrayCount, out mipCount);

            var uavIndex = GetViewIndex(ViewSlice.Single, arrayIndex, mipIndex);
            var uav = UnorderedAccessViews[uavIndex];

            // Creates the unordered access view
            if (uav == null)
            {
                var uavDescription = new UnorderedAccessViewDescription()
                {
                    Format = Description.Format,
                    Dimension = Description.ArraySize > 1 ? UnorderedAccessViewDimension.Texture2DArray : UnorderedAccessViewDimension.Texture2D
                };

                if (Description.ArraySize > 1)
                {
                    uavDescription.Texture2DArray.ArraySize = arrayCount;
                    uavDescription.Texture2DArray.FirstArraySlice = arrayIndex;
                    uavDescription.Texture2DArray.MipSlice = mipIndex;
                }
                else
                {
                    uavDescription.Texture2D.MipSlice = mipIndex;
                }

                uav = new UnorderedAccessView(GraphicsDevice, Resource, uavDescription);
                UnorderedAccessViews[uavIndex] = ToDispose(uav);
            }

            return uav;
        }

        protected override void InitializeViews()
        {
            base.InitializeViews();

            // Creates the shader resource view
            if ((Description.BindFlags & BindFlags.ShaderResource) != 0)
            {
                ShaderResourceViews = new ShaderResourceView[GetViewCount()];

                // Pre initialize by default the view on the first array/mipmap
                GetShaderResourceView(ViewSlice.Full, 0, 0);
            }

            // Creates the unordered access view
            if ((Description.BindFlags & BindFlags.UnorderedAccess) != 0)
            {
                // Initialize the unordered access views
                UnorderedAccessViews = new UnorderedAccessView[GetViewCount()];

                // Pre initialize by default the view on the first array/mipmap
                GetUnorderedAccessView(0, 0);
            }
        }
        
        protected void GetViewSliceBounds(ViewSlice viewSlice, ref int arrayIndex, ref int mipIndex, out int arrayCount, out int mipCount)
        {
            switch (viewSlice)
            {
                case ViewSlice.Full:
                    arrayIndex = 0;
                    mipIndex = 0;
                    arrayCount = Description.ArraySize;
                    mipCount = Description.MipLevels;
                    break;
                case ViewSlice.Single:
                    arrayCount = 1;
                    mipCount = 1;
                    break;
                case ViewSlice.ArrayBand:
                    arrayCount = Description.ArraySize - arrayIndex;
                    mipCount = 1;
                    break;
                case ViewSlice.MipBand:
                    arrayCount = 1;
                    mipCount = Description.MipLevels - mipIndex;
                    break;
                default:
                    arrayCount = 0;
                    mipCount = 0;
                    break;
            }
        }

        protected int GetViewCount()
        {
            return GetViewIndex((ViewSlice)4, Description.ArraySize, Description.MipLevels);
        }

        protected int GetViewIndex(ViewSlice viewSlice, int arrayIndex, int mipIndex)
        {
            return (((int)viewSlice) * Description.ArraySize + arrayIndex) * Description.MipLevels + mipIndex;
        }
        
        protected static DataRectangle[] Pin<T>(int width, PixelFormat format, T[][] initialTextures, out GCHandle[] handles) where T : struct
        {
            var dataRectangles = new DataRectangle[initialTextures.Length];
            handles = new GCHandle[initialTextures.Length];
            for (int i = 0; i < initialTextures.Length; i++)
            {
                var initialTexture = initialTextures[i];
                var handle = GCHandle.Alloc(initialTexture, GCHandleType.Pinned);
                handles[i] = handle;
                dataRectangles[i].DataPointer = handle.AddrOfPinnedObject();
                dataRectangles[i].Pitch = width * format.SizeInBytes;
            }
            return dataRectangles;
        }

        protected static void UnPin(GCHandle[] handles)
        {
            for (int i = 0; i < handles.Length; i++)
                handles[i].Free();
        }

        protected static Texture2DDescription NewDescription(int width, int height, PixelFormat format, bool isReadWrite, int mipCount, int arraySize, ResourceUsage usage)
        {
            var desc = new Texture2DDescription()
                           {
                               Width = width,
                               Height = height,
                               ArraySize = arraySize,
                               SampleDescription = new DXGI.SampleDescription(1, 0),
                               BindFlags = BindFlags.ShaderResource,
                               Format = format,
                               MipLevels = mipCount,
                               Usage = usage,
                               CpuAccessFlags = GetCputAccessFlagsFromUsage(usage)
                           };

            if (isReadWrite)
            {
                desc.BindFlags |= BindFlags.UnorderedAccess;
            }
            return desc;
        }
    }
}