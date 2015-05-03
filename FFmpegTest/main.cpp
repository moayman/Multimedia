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
    // read input file
    unsigned char *buf;
    FILE *fd = fopen(inputfile, "rb");
    fseek (fd , 0 , SEEK_END);
    long lSize = ftell (fd);
    rewind (fd);

    // allocate memory to contain the whole file:
    buf = (unsigned char*) malloc (sizeof(char)*lSize);

    // copy the file into the buffer:
    fread (buf,1,lSize,fd);
    fclose(fd);

    // start the encoding
    AVCodec *codec;
    AVCodecContext *c= NULL;
    AVFrame *frame;
    AVPacket pkt;
    int ret, got_output;
    int buffer_size;
    FILE *f;
    av_register_all();
    avcodec_register_all();
    AVFormatContext *ic = avformat_alloc_context();

    if (avformat_open_input(&ic, inputfile, NULL, NULL) != 0)
    {
        return;
    }


    if (avformat_find_stream_info(ic, NULL) < 0)
    {
        return;
    }

    av_dump_format(ic, 0, "log.txt", 0);

    int i;
    AVCodecContext *pCodecCtxOrig = NULL;
    AVCodecContext *pCodecCtx = NULL;

    // Find the first video stream
    int videoStream=-1;
    for(i=0; i<ic->nb_streams; i++)
      if(ic->streams[i]->codec->codec_type==AVMEDIA_TYPE_AUDIO) {
        videoStream=i;
        break;
      }
    if(videoStream==-1)
      return; // Didn't find a video stream

    // Get a pointer to the codec context for the video stream
    pCodecCtx=ic->streams[videoStream]->codec;


    // TODO: use AV_CODEC_ID_MP3, its not available!!
    codec = avcodec_find_encoder(pCodecCtx->codec_id);

    fprintf(stderr, "%10c Codec not found\n", *(pCodecCtx->codec_name));
    if (!codec)
    {
        fprintf(stderr, "%d Codec not found\n", ic->audio_codec_id);
        return;
    }

    c = avcodec_alloc_context3(codec);
    if (!c)
    {
        fprintf(stderr, "Could not allocate audio codec context\n");
        return;
    }

    /* put sample parameters */
    c->bit_rate = ic->bit_rate;
    //ic->

    /* check that the encoder supports s16 pcm input */
    c->sample_fmt = AV_SAMPLE_FMT_S16;

    if (!check_sample_fmt(codec, c->sample_fmt))
    {
        fprintf(stderr, "Encoder does not support sample format %s",
                av_get_sample_fmt_name(c->sample_fmt));
        return;
    }
    /* select other audio parameters supported by the encoder */
    c->sample_rate    = select_sample_rate(codec);
    c->channel_layout = select_channel_layout(codec);
    c->channels       = av_get_channel_layout_nb_channels(c->channel_layout);
    /* open it */
    if (avcodec_open2(c, codec, NULL) < 0)
    {
        fprintf(stderr, "Could not open codec\n");
        return;
    }
    f = fopen(filename, "wb");
    if (!f)
    {
        fprintf(stderr, "Could not open %s\n", filename);
        return;
    }
    /* frame containing input raw audio */
    frame = av_frame_alloc();
    if (!frame) {
        fprintf(stderr, "Could not allocate audio frame\n");
        return;
    }
    frame->nb_samples     = c->frame_size;
    frame->format         = c->sample_fmt;
    frame->channel_layout = c->channel_layout;
    /* the codec gives us the frame size, in samples,
     * we calculate the size of the samples buffer in bytes */
    buffer_size = av_samples_get_buffer_size(NULL, c->channels, c->frame_size,
                                             c->sample_fmt, 0);
    if (buffer_size < 0)
    {
        fprintf(stderr, "Could not get sample buffer size\n");
        return;
    }

    ret = avcodec_fill_audio_frame(frame, c->channels, c->sample_fmt,
                                   (const uint8_t*)buf, buffer_size, 0);
    if (ret < 0) {
        fprintf(stderr, "Could not setup audio frame\n");
        return;
    }

    // FIXME: loop till what?
    for (int i = 0; i < 50; i++) {
        av_init_packet(&pkt);
        pkt.data = NULL;
        pkt.size = 0;


        // actuall encoding
        ret = avcodec_encode_audio2(c, &pkt, frame, &got_output);
        if (ret < 0)
        {
            fprintf(stderr, "Error encoding audio frame\n");
            return;
        }

        // write encoded packet to output file
        if (got_output)
        {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }

    /* get the delayed frames */
    for (got_output = 1; got_output;)
    {
        ret = avcodec_encode_audio2(c, &pkt, NULL, &got_output);
        if (ret < 0)
        {
            fprintf(stderr, "Error encoding frame\n");
            return;
        }
        if (got_output)
        {
            fwrite(pkt.data, 1, pkt.size, f);
            av_free_packet(&pkt);
        }
    }

    fclose(f);
    delete buf;
    //av_freep(&samples);
    av_frame_free(&frame);
    avcodec_close(c);
    av_free(c);
}
