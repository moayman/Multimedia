#include <cstdio>
#include <cstdlib>

#ifdef __cplusplus
extern "C" {
#endif
    #include <libavcodec/avcodec.h>
    #include <libavformat/avformat.h>
    #include <libavutil/opt.h>
    #include <libavutil/channel_layout.h>
    #include <libavutil/common.h>
    #include <libavutil/imgutils.h>
    #include <libavutil/mathematics.h>
    #include <libavutil/samplefmt.h>

#ifdef __cplusplus
}
#endif

void audio_encode_example(char*inputfile, char *filename);
int select_channel_layout(AVCodec *codec);
int select_sample_rate(AVCodec *codec);
int check_sample_fmt(AVCodec *codec, enum AVSampleFormat sample_fmt);

int main(int argc, char *argv[])
{


    audio_encode_example("bullet.wav", "bullet.mp2");

    //if (argc == 3)
    //    audio_encode_example(argv[1], argv[2]);
    //else
    //    printf("-usage: ./libcodec [inputfile].wav [outputfile].mp3");
}

int check_sample_fmt(AVCodec *codec, enum AVSampleFormat sample_fmt)
{
    const enum AVSampleFormat *p = codec->sample_fmts;
    while (*p != AV_SAMPLE_FMT_NONE) {
        if (*p == sample_fmt)
            return 1;
        p++;
    }
    return 0;
}

/* just pick the highest supported samplerate */
 int select_sample_rate(AVCodec *codec)
{
    const int *p;
    int best_samplerate = 0;
    if (!codec->supported_samplerates)
        return 44100;
    p = codec->supported_samplerates;
    while (*p) {
        best_samplerate = FFMAX(*p, best_samplerate);
        p++;
    }
    return best_samplerate;
}
/* select layout with the highest channel count */
 int select_channel_layout(AVCodec *codec)
{
    const uint64_t *p;
    uint64_t best_ch_layout = 0;
    int best_nb_channels   = 0;
    if (!codec->channel_layouts)
        return AV_CH_LAYOUT_STEREO;
    p = codec->channel_layouts;
    while (*p) {
        int nb_channels = av_get_channel_layout_nb_channels(*p);
        if (nb_channels > best_nb_channels) {
            best_ch_layout    = *p;
            best_nb_channels = nb_channels;
        }
        p++;
    }
    return best_ch_layout;
}



void audio_encode_example(char*inputfile, char *filename)
{
    avcodec_register_all();
    av_register_all();

    FILE *f = fopen(filename, "wb");


    // start the encoding
    AVCodec * ptr_codec;
    AVCodecContext *ptr_codec_context = NULL;
    AVCodecContext * ptr_codec_context2 = NULL;
    AVFrame * ptr_frame;
    AVPacket pkt;

    AVCodec * ptr_codec_mp2;
    AVCodecContext *ptr_codec_mp2_context = NULL;

    int ret, got_output;


    AVFormatContext *ptr_format_context = avformat_alloc_context();

    if (avformat_open_input(&ptr_format_context, inputfile, NULL, NULL) != 0)
    {
        fprintf(stderr, "failed to open the file\n");
        return;
    }


    if (avformat_find_stream_info(ptr_format_context, NULL) < 0)
    {
        return;
    }

    // dump the file
    av_dump_format(ptr_format_context, 0, "log.txt", 0);



    //int audio_stream_index = -1;

    //for (int i = 0; i < ptr_format_context->nb_streams; i++)
        // check for the first stream of type audio
        // FIXME: can be removed, I guess!!
    //    if (ptr_format_context->streams[i]->codec->codec_type == AVMEDIA_TYPE_AUDIO)
    //    {
    //        audio_stream_index=i;
    //        break;
    //    }

    //if (audio_stream_index == -1)
    //{
    //    fprintf(stderr, "couldn't find audio stream\n");
    //    return;
    //}

    AVStream *d = ptr_format_context->streams[0];
    printf("%d", d->duration);
    ptr_codec_context = ptr_format_context->streams[0]->codec;


    // find the suitable codec
    ptr_codec = avcodec_find_encoder(ptr_codec_context->codec_id);
    if (!ptr_codec)
    {
        fprintf(stderr, "%d Codec not found\n", ptr_codec_context->codec_id);
        return;
    }

    // allocate codec context
    ptr_codec_context = avcodec_alloc_context3(ptr_codec);
    if (!ptr_codec_context)
    {
        fprintf(stderr, "Could not allocate audio codec context\n");
        return;
    }

    //ptr_frame- = ptr_format_context->bit_rate;
    //ptr_codec_context->format = ptr_format_context->;
    ptr_codec_context->channel_layout = ptr_codec_context->channel_layout;
    ptr_codec_context->sample_fmt = AV_SAMPLE_FMT_S16;
    // copy the context to a new one
    //if (avcodec_copy_context(ptr_codec_context, ptr_codec_context2) != 0)
    //{
    //    fprintf(stderr, "failed to copy the context\n");
    //    return;
    //}

    if (avcodec_open2(ptr_codec_context, ptr_codec, NULL) < 0)
    {
        fprintf(stderr, "failed to open the codec\n");
        return;
    }


    // frame will contain the audio raw values
    ptr_frame = av_frame_alloc();

    ptr_frame->channels = ptr_codec_context->channels;
    ptr_frame->format = ptr_codec_context->sample_fmt;
    ptr_frame->channel_layout = ptr_codec_context->channel_layout;

    uint8_t * buffer = NULL;
    int buffer_size;

    // Determine required buffer size and allocate buffer
    buffer_size =  av_samples_get_buffer_size(NULL, ptr_codec_context->channels, ptr_codec_context->frame_size, ptr_codec_context->sample_fmt, 0);
    buffer = (uint8_t *) av_malloc(buffer_size);
printf("%d", buffer_size);

    if (avcodec_fill_audio_frame(ptr_frame, ptr_codec_context->channels, ptr_codec_context->sample_fmt, buffer, buffer_size, 0) < 0)
    {
        fprintf(stderr, "couldn't setup audio frame\n");
        return;
    }

    // open the mp2 codec
    ptr_codec_mp2 = avcodec_find_encoder(AV_CODEC_ID_MP2);
    ptr_codec_mp2_context = avcodec_alloc_context3(ptr_codec_mp2);
    if (!ptr_codec_mp2_context)
    {
        fprintf(stderr, "Could not allocate audio codec context\n");
        return;
    }

    ptr_codec_mp2_context->bit_rate = ptr_codec_context->bit_rate;
    ptr_codec_mp2_context->sample_fmt = AV_SAMPLE_FMT_S16;
    ptr_codec_mp2_context->sample_rate = select_sample_rate(ptr_codec_mp2);
    ptr_codec_mp2_context->channel_layout = select_channel_layout(ptr_codec_mp2);
    ptr_codec_mp2_context->channels = av_get_channel_layout_nb_channels(ptr_codec_mp2_context->channel_layout);

    if (avcodec_open2(ptr_codec_mp2_context, ptr_codec_mp2, NULL) < 0 )
    {
        fprintf(stderr, "Could not open codec\n");
        return;
    }

    for (int i=0; i<buffer_size; i++)
    {

        av_init_packet(&pkt);
        pkt.data = NULL; // packet data will be allocated by the encoder
        pkt.size = 0;

        ret = avcodec_encode_audio2(ptr_codec_mp2_context, &pkt, ptr_frame, &got_output);
        if (ret < 0)
        {
            fprintf(stderr, "Error encoding audio frame\n");
            exit(1);
        }
        if (got_output)
        {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }

    /* get the delayed frames */
    for (got_output = 1; got_output;)
    {
        ret = avcodec_encode_audio2(ptr_codec_mp2_context, &pkt, NULL, &got_output);
        if (ret < 0)
        {
            fprintf(stderr, "Error encoding frame\n");
            exit(1);
        }
        if (got_output)
        {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }

    fclose(f);
    av_freep(&buffer);
    avcodec_free_frame(&ptr_frame);
    avcodec_close(ptr_codec_context2);
    avcodec_close(ptr_codec_context);
    av_free(ptr_codec_context);
    av_free(ptr_codec_context2);
    av_free(ptr_format_context);

}
