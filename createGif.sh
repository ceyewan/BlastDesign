#!/bin/bash

# 检查是否安装了 ffmpeg
if ! command -v ffmpeg &>/dev/null; then
  echo "错误：未检测到 FFmpeg。请先安装 FFmpeg 后再运行此脚本。"
  echo "在 Mac 上，可以通过 Homebrew 安装：brew install ffmpeg"
  exit 1
fi

# 检查 frames 文件夹是否存在
if [ ! -d "frames" ]; then
  echo "错误：未找到 frames 文件夹。请确保图片序列存放在当前目录下的 frames 文件夹中。"
  exit 1
fi

# 检查 images 文件夹是否存在
if [ ! -d "images" ]; then
  echo "错误：images 文件夹不存在。请确保 images 文件夹存在。"
  exit 1
fi

# 删除已有的 palette.png
if [ -f "./images/palette.png" ]; then
  rm ./images/palette.png
fi

# 生成调色板
echo "生成调色板文件 palette.png..."
ffmpeg -i frames/%03d.png -vf "palettegen" ./images/palette.png
if [ $? -ne 0 ]; then
  echo "错误：调色板生成失败，请检查图片序列文件是否正确命名为 001.png, 002.png 等。"
  exit 1
fi

# 删除已有的 blast_time.gif
if [ -f "./images/blast_time.gif" ]; then
  rm ./images/blast_time.gif
fi

# 让用户输入帧率
echo "请输入帧率（默认为 4）："
read framerate

# 如果用户未输入帧率，则默认为4
framerate=${framerate:-4}

# 生成 GIF
echo "生成动画 GIF..."
ffmpeg -framerate "$framerate" -i frames/%03d.png -i ./images/palette.png -lavfi "paletteuse" ./images/blast_time.gif
if [ $? -ne 0 ]; then
  echo "错误：GIF 生成失败，请检查输入文件。"
  exit 1
fi

echo "操作完成！生成的动画文件为 output.gif。"