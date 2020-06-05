using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ByteBuffer;
using UnityEngine;
using IntPtr = System.IntPtr;


public delegate void OnBufferReturned(ByteMemory buffer);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void FreeCallback(byte* data, void* userData);

static class Dav1dConstants
{
    public const int Dav1DMaxCdefStrengths = 8;
    public const int Dav1DMaxOperatingPoints = 32;
    public const int Dav1DMaxTileCols = 64;
    public const int Dav1DMaxTileRows = 64;
    public const int Dav1DMaxSegments = 8;
    public const int Dav1DNumRefFrames = 8;
    public const int Dav1DPrimaryRefNone = 7;
    public const int Dav1DRefsPerFrame = 7;
    public const int Dav1DTotalRefsPerFrame = (Dav1DRefsPerFrame + 1);
}

public unsafe struct Dav1dRef
{
    public void* data;
    public void* constData;
    public int refCnt;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public FreeCallback freeCallback;
    public void* userData;
}

public unsafe struct Dav1dUserData
{
    public byte* data; // data pointer
    public Dav1dRef @ref; // allocation origin
}

public struct Dav1dDataProps
{
    public long timestamp; // container timestamp of input data, INT64_MIN if unknown (default)
    public long duration; // container duration of input data, 0 if unknown (default)
    public long offset; // stream offset of input data, -1 if unknown (default)
    public UIntPtr size; // packet size, default Dav1dData.sz
    public Dav1dUserData userData; // user-configurable data, default NULL members
}

public unsafe struct Dav1dData
{
    public byte[] data; // data pointer
    public UIntPtr sz; // data size
    public Dav1dRef @ref; // allocation origin
    public Dav1dDataProps m; // user provided metadata passed to the output picture
}

public struct Dav1dPicAllocator
{
    public UIntPtr cookie;
    public UIntPtr allocPictureCallback;
    public UIntPtr releasePictureCallback;
    //int (* alloc_picture_callback) (Dav1dPicture* pic, void* cookie);
    //void (* release_picture_callback) (Dav1dPicture* pic, void* cookie);
}

public struct Dav1dLogger
{
    public UIntPtr cookie;
    public UIntPtr callback;
    //void (* callback) (void* cookie, const char* format, va_list ap);
}

public struct Dav1dSettings
{
    public int nFrameThreads;
    public int nTileThreads;
    public int applyGrain;
    public int operatingPoint; // select an operating point for scalable AV1 bitstreams (0 - 31)
    public int allLayers; // output all spatial layers of a scalable AV1 biststream
    public uint frameSizeLimit; // maximum frame size, in pixels (0 = unlimited)
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] reserved; // reserved for future use
    public Dav1dPicAllocator allocator; // Picture allocator callback.
    public Dav1dLogger logger; // Logger callback.
}


public enum Dav1dPixelLayout
{
    Dav1DPixelLayoutI400, // monochrome
    Dav1DPixelLayoutI420, // 4:2:0 planar
    Dav1DPixelLayoutI422, // 4:2:2 planar
    Dav1DPixelLayoutI444, // 4:4:4 planar
};

public enum Dav1dFrameType
{
    Dav1DFrameTypeKey = 0,    // Key Intra frame
    Dav1DFrameTypeInter = 1,  // Inter frame
    Dav1DFrameTypeIntra = 2,  // Non key Intra frame
    Dav1DFrameTypeSwitch = 3, // Switch Inter frame
};

public enum Dav1dColorPrimaries
{
    Dav1DColorPriBt709 = 1,
    Dav1DColorPriUnknown = 2,
    Dav1DColorPriBt470M = 4,
    Dav1DColorPriBt470Bg = 5,
    Dav1DColorPriBt601 = 6,
    Dav1DColorPriSmpte240 = 7,
    Dav1DColorPriFilm = 8,
    Dav1DColorPriBt2020 = 9,
    Dav1DColorPriXyz = 10,
    Dav1DColorPriSmpte431 = 11,
    Dav1DColorPriSmpte432 = 12,
    Dav1DColorPriEbu3213 = 22,
};

public enum Dav1dTransferCharacteristics
{
    Dav1DTrcBt709 = 1,
    Dav1DTrcUnknown = 2,
    Dav1DTrcBt470M = 4,
    Dav1DTrcBt470Bg = 5,
    Dav1DTrcBt601 = 6,
    Dav1DTrcSmpte240 = 7,
    Dav1DTrcLinear = 8,
    Dav1DTrcLog100 = 9,         // logarithmic (100:1 range)
    Dav1DTrcLog100Sqrt10 = 10, // lograithmic (100*sqrt(10):1 range)
    Dav1DTrcIec61966 = 11,
    Dav1DTrcBt1361 = 12,
    Dav1DTrcSrgb = 13,
    Dav1DTrcBt202010Bit = 14,
    Dav1DTrcBt202012Bit = 15,
    Dav1DTrcSmpte2084 = 16,     // PQ
    Dav1DTrcSmpte428 = 17,
    Dav1DTrcHlg = 18,           // hybrid log/gamma (BT.2100 / ARIB STD-B67)
};

public enum Dav1dMatrixCoefficients
{
    Dav1DMcIdentity = 0,
    Dav1DMcBt709 = 1,
    Dav1DMcUnknown = 2,
    Dav1DMcFcc = 4,
    Dav1DMcBt470Bg = 5,
    Dav1DMcBt601 = 6,
    Dav1DMcSmpte240 = 7,
    Dav1DMcSmpteYcgco = 8,
    Dav1DMcBt2020Ncl = 9,
    Dav1DMcBt2020Cl = 10,
    Dav1DMcSmpte2085 = 11,
    Dav1DMcChromatNcl = 12, // Chromaticity-derived
    Dav1DMcChromatCl = 13,
    Dav1DMcIctcp = 14,
};

public enum Dav1dChromaSamplePosition
{
    Dav1DChrUnknown = 0,
    Dav1DChrVertical = 1,  // Horizontally co-located with luma(0, 0)
                           // sample, between two vertical samples
    Dav1DChrColocated = 2, // Co-located with luma(0, 0) sample
};

public enum Dav1dAdaptiveBoolean
{
    Dav1DOff = 0,
    Dav1DOn = 1,
    Dav1DAdaptive = 2,
};

public struct Dav1dSequenceHeader
{
    /**
     * Stream profile, 0 for 8-10 bits/component 4:2:0 or monochrome;
     * 1 for 8-10 bits/component 4:4:4; 2 for 4:2:2 at any bits/component,
     * or 12 bits/component at any chroma subsampling.
     */
    public int profile;
    /**
     * Maximum dimensions for this stream. In non-scalable streams, these
     * are often the actual dimensions of the stream, although that is not
     * a normative requirement.
     */
    public int maxWidth, maxHeight;
    public Dav1dPixelLayout layout; // format of the picture
    public Dav1dColorPrimaries pri; // color primaries (av1)
    public Dav1dTransferCharacteristics trc; // transfer characteristics (av1)
    public Dav1dMatrixCoefficients mtrx; // matrix coefficients (av1)
    public Dav1dChromaSamplePosition chr; // chroma sample position (av1)
    /**
     * 0, 1 and 2 mean 8, 10 or 12 bits/component, respectively. This is not
     public * exactly the same as 'hbd' from the spec; the spec's hbd distinguishes
     * between 8 (0) and 10-12 (1) bits/component, and another element
     * (twelve_bit) to distinguish between 10 and 12 bits/component. To get
     * the spec's hbd, use !!our_hbd, and to get twelve_bit, use hbd == 2.
     */
    public int hbd;
    /**
     * Pixel data uses JPEG pixel range ([0,255] for 8bits) instead of
     * MPEG pixel range ([16,235] for 8bits luma, [16,240] for 8bits chroma).
     */
    public int colorRange;

    public int numOperatingPoints;
    public struct Dav1dSequenceHeaderOperatingPoint
    {
        public int majorLevel, minorLevel;
        public int initialDisplayDelay;
        public int idc;
        public int tier;
        public int decoderModelParamPresent;
        public int displayModelParamPresent;
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Dav1dConstants.Dav1DMaxOperatingPoints)]
    public Dav1dSequenceHeaderOperatingPoint[] operatingPoints;

    public int stillPicture;
    public int reducedStillPictureHeader;
    public int timingInfoPresent;
    public int numUnitsInTick;
    public int timeScale;
    public int equalPictureInterval;
    public uint numTicksPerPicture;
    public int decoderModelInfoPresent;
    public int encoderDecoderBufferDelayLength;
    public int numUnitsInDecodingTick;
    public int bufferRemovalDelayLength;
    public int framePresentationDelayLength;
    public int displayModelInfoPresent;
    public int widthNBits, heightNBits;
    public int frameIdNumbersPresent;
    public int deltaFrameIdNBits;
    public int frameIdNBits;
    public int sb128;
    public int filterIntra;
    public int intraEdgeFilter;
    public int interIntra;
    public int maskedCompound;
    public int warpedMotion;
    public int dualFilter;
    public int orderHint;
    public int jntComp;
    public int refFrameMvs;
    public Dav1dAdaptiveBoolean screenContentTools;
    public Dav1dAdaptiveBoolean forceIntegerMv;
    public int orderHintNBits;
    public int superRes;
    public int cdef;
    public int restoration;
    public int ssHor, ssVer, monochrome;
    public int colorDescriptionPresent;
    public int separateUvDeltaQ;
    public int filmGrainPresent;

    // Dav1dSequenceHeaders of the same sequence are required to be
    // bit-identical until this offset. See 7.5 "Ordering of OBUs":
    //   Within a particular coded video sequence, the contents of
    //   sequence_header_obu must be bit-identical each time the
    //   sequence header appears except for the contents of
    //   operating_parameters_info.
    public struct Dav1dSequenceHeaderOperatingParameterInfo
    {
        public int decoderBufferDelay;
        public int encoderBufferDelay;
        public int lowDelayMode;
    }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Dav1dConstants.Dav1DMaxOperatingPoints)]
    public Dav1dSequenceHeaderOperatingParameterInfo[] operatingParameterInfo;
}

public unsafe struct Dav1dFilmGrainData
{
    uint _seed;
    int _numYPoints;
    public fixed byte yPoints[14 * 2 /* value, scaling */];
    int _chromaScalingFromLuma;
    public fixed int numUvPoints[2];
    public fixed byte uvPoints[2 * 10 * 2 /* value, scaling */];
    int _scalingShift;
    int _arCoeffLag;
    public fixed sbyte arCoeffsY[24];
    public fixed sbyte arCoeffsUv[2 * 25 + 3 /* padding for alignment purposes */];
    ulong _arCoeffShift;
    int _grainScaleShift;
    public fixed int uvMult[2];
    public fixed int uvLumaMult[2];
    public fixed int uvOffset[2];
    int _overlapFlag;
    int _clipToRestrictedRange;
}
enum Dav1dFilterMode
{
    DAV1D_FILTER_8TAP_REGULAR,
    DAV1D_FILTER_8TAP_SMOOTH,
    DAV1D_FILTER_8TAP_SHARP,
    DAV1D_N_SWITCHABLE_FILTERS,
    DAV1D_FILTER_BILINEAR = DAV1D_N_SWITCHABLE_FILTERS,
    DAV1D_N_FILTERS,
    DAV1D_FILTER_SWITCHABLE = DAV1D_N_FILTERS,
};

struct Dav1dSegmentationData
{
    int delta_q;
    int delta_lf_y_v, delta_lf_y_h, delta_lf_u, delta_lf_v;
    int @ref;
    int skip;
    int globalmv;
}

struct Dav1dSegmentationDataSet
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Dav1dConstants.Dav1DMaxSegments)]
    Dav1dSegmentationData[] d;
    int preskip;
    int last_active_segid;
}

public unsafe struct Dav1dLoopfilterModeRefDeltas
{
    fixed int mode_delta[2 /* is_zeromv */];
    fixed int ref_delta[Dav1dConstants.Dav1DTotalRefsPerFrame];
}

enum Dav1dRestorationType
{
    DAV1D_RESTORATION_NONE,
    DAV1D_RESTORATION_SWITCHABLE,
    DAV1D_RESTORATION_WIENER,
    DAV1D_RESTORATION_SGRPROJ,
};
enum Dav1dTxfmMode
{
    DAV1D_TX_4X4_ONLY,
    DAV1D_TX_LARGEST,
    DAV1D_TX_SWITCHABLE,
    DAV1D_N_TX_MODES,
}

public unsafe struct Dav1dFrameHeader
{
    public struct FilmGrain
    {
        Dav1dFilmGrainData _data;
        int _present, _update;
    }
    FilmGrain _filmGrain; // film grain parameters
    Dav1dFrameType frame_type; // type of the picture
    public fixed int _width[2 /* { coded_width, superresolution_upscaled_width } */];
    int _height;
    int _frameOffset; // frame number
    int _temporalId; // temporal id of the frame for SVC
    int _spatialId; // spatial id of the frame for SVC

    int _showExistingFrame;
    int _existingFrameIdx;
    int _frameId;
    int _framePresentationDelay;
    int _showFrame;
    int _showableFrame;
    int _errorResilientMode;
    int _disableCdfUpdate;
    int _allowScreenContentTools;
    int _forceIntegerMv;
    int _frameSizeOverride;
    int _primaryRefFrame;
    int _bufferRemovalTimePresent;
    public fixed int operating_points__bufferRemovalTime[Dav1dConstants.Dav1DMaxOperatingPoints];
    int _refreshFrameFlags;
    int _renderWidth, _renderHeight;
    struct SuperResolution
    {
        int widthScaleDenominator;
        int enabled;
    }
    SuperResolution super_res;
    int _haveRenderSize;
    int _allowIntrabc;
    int _frameRefShortSignaling;
    public fixed int _refidx[Dav1dConstants.Dav1DRefsPerFrame];
    int _hp;
    Dav1dFilterMode subpel_filter_mode;
    int _switchableMotionMode;
    int _useRefFrameMvs;
    int _refreshContext;
    struct Tiling
    {
        int uniform;
        uint nBytes;
        int minLog2Cols, maxLog2Cols, log2Cols, cols;
        int minLog2Rows, maxLog2Rows, log2Rows, rows;
        public fixed ushort col_start_sb[Dav1dConstants.Dav1DMaxTileCols + 1];
        public fixed ushort row_start_sb[Dav1dConstants.Dav1DMaxTileCols + 1];
        int update;
    }
    Tiling tiling;
    struct Quant
    {
        int yac;
        int ydcDelta;
        int udcDelta, uacDelta, vdcDelta, vacDelta;
        int qm, qmY, qmU, qmV;
    }
    Quant quant;
    struct Segmentation
    {
        int enabled, updateMap, temporal, updateData;
        Dav1dSegmentationDataSet segData;
        public fixed int lossless[Dav1dConstants.Dav1DMaxSegments], qidx[Dav1dConstants.Dav1DMaxSegments];
    }
    Segmentation segmentation;
    struct Delta
    {
        struct Q
        {
            int present;
            int resLog2;
        }
        Q q;
        struct Lf
        {
            int present;
            int resLog2;
            int multi;
        }
        Lf lf;
    }
    Delta delta;
    int _allLossless;
    struct Loopfilter
    {
        fixed int level_y[2 /* dir */];
        int levelU, levelV;
        int modeRefDeltaEnabled;
        int modeRefDeltaUpdate;
        Dav1dLoopfilterModeRefDeltas modeRefDeltas;
        int sharpness;
    }
    Loopfilter loopfilter;
    struct Cdef
    {
        int damping;
        int nBits;
        fixed int y_strength[Dav1dConstants.Dav1DMaxCdefStrengths];
        fixed int uv_strength[Dav1dConstants.Dav1DMaxCdefStrengths];
    }
    Cdef cdef;
    struct Restoration
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        Dav1dRestorationType[] type;
        fixed int unit_size[2 /* y, uv */];
    }
    Restoration restoration;
    Dav1dTxfmMode txfm_mode;
    int _switchableCompRefs;
    int _skipModeAllowed, _skipModeEnabled;
    fixed int _skipModeRefs[2];
    int _warpMotion;
    int _reducedTxtpSet;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Dav1dConstants.Dav1DRefsPerFrame)]
    Dav1dWarpedMotionParams[] _gmv;
}

enum Dav1dWarpedMotionType
{
    DAV1D_WM_TYPE_IDENTITY,
    DAV1D_WM_TYPE_TRANSLATION,
    DAV1D_WM_TYPE_ROT_ZOOM,
    DAV1D_WM_TYPE_AFFINE,
};

unsafe struct Dav1dWarpedMotionParams
{
    Dav1dWarpedMotionType type;
    fixed int matrix[6];
    short alpha, beta, gamma, delta;
}

struct Dav1dPictureParameters
{
    int w; ///< width (in pixels)
    int h; ///< height (in pixels)
    Dav1dPixelLayout layout; ///< format of the picture
    int bpc; ///< bits per pixel component (8 or 10)
}
struct Dav1dContentLightLevel
{
    int max_content_light_level;
    int max_frame_average_light_level;
}
unsafe struct Dav1dMasteringDisplay
{
    ///< 0.16 fixed point
    fixed ushort primaries[3 * 2];
    ///< 0.16 fixed point
    fixed ushort white_point[2];
    ///< 24.8 fixed point
    uint max_luminance;
    ///< 18.14 fixed point
    uint min_luminance;
}
struct Dav1dITUTT35
{
    byte country_code;
    byte country_code_extension_byte;
    UIntPtr payload_size;
    UIntPtr payload;
}

public struct Dav1dPicture
{
    Dav1dSequenceHeader _seqHdr;
    Dav1dFrameHeader _frameHdr;

    /**
     * Pointers to planar image data (Y is [0], U is [1], V is [2]). The data
     * should be bytes (for 8 bpc) or words (for 10 bpc). In case of words
     * containing 10 bpc image data, the pixels should be located in the LSB
     * bits, so that values range between [0, 1023]; the upper bits should be
     * zero'ed out.
     */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    UIntPtr[] _data;

    /**
     * Number of bytes between 2 lines in data[] for luma [0] or chroma [1].
     */
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    UIntPtr[] _stride;

    Dav1dPictureParameters _p;
    Dav1dDataProps _m;

    /**
     * High Dynamic Range Content Light Level metadata applying to this picture,
     * as defined in section 5.8.3 and 6.7.3
     */
    Dav1dContentLightLevel _contentLight;
    /**
     * High Dynamic Range Mastering Display Color Volume metadata applying to
     * this picture, as defined in section 5.8.4 and 6.7.4
     */
    Dav1dMasteringDisplay _masteringDisplay;
    /**
     * ITU-T T.35 metadata as defined in section 5.8.2 and 6.7.2
     */
    Dav1dITUTT35 _itutT35;
    
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    UIntPtr[] _reserved; // reserved for future use

    Dav1dRef frame_hdr_ref; // Dav1dFrameHeader allocation origin
    Dav1dRef seq_hdr_ref; // Dav1dSequenceHeader allocation origin
    Dav1dRef content_light_ref; // Dav1dContentLightLevel allocation origin
    Dav1dRef mastering_display_ref; // Dav1dMasteringDisplay allocation origin
    Dav1dRef itut_t35_ref; // Dav1dITUTT35 allocation origin
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    UIntPtr[] _reservedRef; // reserved for future use
    Dav1dRef @ref; // Frame data allocation origin

    UIntPtr _allocatorData; // pointer managed by the allocator
}

public class Dav1dDecoder
{
    public static Dav1dDecoder Instance { get; } = new Dav1dDecoder();
    private readonly Dictionary<IntPtr, ByteMemory> _memories = new Dictionary<IntPtr, ByteMemory>();
    private readonly UIntPtr _dav1dContext;
    public event OnBufferReturned BufferReturned;

    [DllImport("libdav1d")]
    private static extern unsafe int dav1d_data_wrap(Dav1dData data, byte* buffer, UIntPtr size,
        FreeCallback freeCallback, IntPtr cookie);
    [DllImport("libdav1d")]
    private static extern void dav1d_default_settings(ref Dav1dSettings s);
    [DllImport("libdav1d")]
    private static extern int dav1d_open(ref UIntPtr contextOut, ref Dav1dSettings s);
    [DllImport("libdav1d")]
    private static extern int dav1d_get_picture(UIntPtr dav1dContext, out Dav1dPicture picture);



    public Dav1dDecoder()
    {
        var setting = new Dav1dSettings();
        dav1d_default_settings(ref setting);
        _dav1dContext = new UIntPtr();
        var result = dav1d_open(ref _dav1dContext, ref setting);
        if (result != 0)
        {
            throw new Exception($"Fail to open dav1d {result}");
        }
    }

    public void SendData(ByteMemory buffer)
    {
        unsafe
        {
            var bytes = (byte*)buffer.Pointer.ToPointer();
            _memories.Add((IntPtr)bytes, buffer);
            var data = new Dav1dData
            {
                m = new Dav1dDataProps(),
            };
            dav1d_data_wrap(data, bytes, (UIntPtr)buffer.Length, FreeCallback, IntPtr.Zero);
        }
    }

    private unsafe void FreeCallback(byte* data, void* _)
    {
        throw new NotImplementedException();
    }

    public bool TryGetNextFrameTexture(out Texture frameTexture)
    {
        var result = dav1d_get_picture(_dav1dContext, out var picture);
        if (result < 0)
        {
            const int EAgain = -11;
            if (result == EAgain)
            {
                frameTexture = null;
                return false;
            }

            throw new Exception($"fail to get next frame {result}");
        }

        throw new NotImplementedException();
    }

    public void ReturnFrameTexture(Texture frameTexture)
    {

    }
}
