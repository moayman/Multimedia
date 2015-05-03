#-------------------------------------------------
#
# Project created by QtCreator 2015-04-29T19:32:40
#
#-------------------------------------------------

QT       += core

QT       -= gui

TARGET = FFmpegTest
CONFIG   += console
CONFIG   -= app_bundle

TEMPLATE = app

LIBS += -lavformat -lavcodec -lavutil -lswresample -lz -lm -llzma

SOURCES += main.cpp
