using NDIPlugin;
using UnityEngine;

namespace Klak.Ndi
{
    sealed class FormatConverter : System.IDisposable
    {
        private ComputeBuffer _encoderOutput;
        private ComputeShader _encoderCompute;

        public FormatConverter(ComputeShader encoderCompute)
        {
            _encoderCompute = encoderCompute;
        }

        public void Dispose() => ReleaseBuffers();

        private void ReleaseBuffers()
        {
            _encoderOutput?.Dispose();
            _encoderOutput = null;
        }


        public ComputeBuffer Encode(Texture source, bool enableAlpha, bool vflip)
        {
            int width = source.width;
            int height = source.height;
            int dataCount = Utils.FrameDataCount(width, height, enableAlpha);

            // Reallocate the output buffer when the output size was changed.
            if (_encoderOutput != null && _encoderOutput.count != dataCount)
            {
                ReleaseBuffers();
            }

            // Output buffer allocation
            if (_encoderOutput == null)
            {
                _encoderOutput = new ComputeBuffer(dataCount, 4);
            }

            // Compute thread dispatching
            int pass = enableAlpha ? 1 : 0;
            _encoderCompute.SetInt("VFlip", vflip ? -1 : 1);
            _encoderCompute.SetTexture(pass, "Source", source);
            _encoderCompute.SetBuffer(pass, "Destination", _encoderOutput);
            _encoderCompute.Dispatch(pass, width / 16, height / 8, 1);

            return _encoderOutput;
        }
    }
}