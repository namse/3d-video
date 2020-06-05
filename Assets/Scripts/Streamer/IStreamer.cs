using System;
using System.Threading.Tasks;
using Amazon.S3.Model;
using ByteBuffer;

public delegate void OnStreamChunkReceived(ByteMemory chunk);

public interface IStreamer
{
    // from s3, from stream
    Task StreamFileAsync(GetObjectRequest request, int minChunkSize,
        int maxChunkSize, OnStreamChunkReceived onStreamChunkReceived);
}

