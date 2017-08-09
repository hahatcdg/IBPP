# IBPP
Image-based promoter prediction

IBPP (Image-based promoter prediction) is a method used to detect transcriptional promoters. The program uses a collection of transcription start sites (TSSs) to generate an “image”. By matching a sequence with the “image”, the program can distinguish promoters from other sequences. The program is species specific, because the “image” is generated using TSSs from a certain species, such as E. coli. In order to predict promoters in other species, specific “image” should be generated using TSSs of that species.

This repository contains the first version of IBPP. It includes 3 directories: “ImageGeneration”, “IBPP” and “IBPP-SVM”. 

The directory “ImageGeneration” contains the tool used to generate “images” from TSSs. To use it, 2 files (“P.txt”, “Ref.txt”) should be present in the same directory with “ImageGeneration.exe”. “P.txt” contains a collection of TSSs, while “Ref.txt” contains a collection of non-promoter sequences randomly generated using CDSs. Notice each time the program will pick a max of 500 sequences from each collection. If the collection contains more than 500 sequences, the program will randomly pick 500 from it. The generated “images” can be found in new directories such as “\Output1\OutputFiles\”. After the 100th generation, the best “image” would be written in the file “Pattern.txt”.

The directory “IBPP” contains the tool that uses one of the “images” to analyze any given sequence by matching with it. To use it, an “image” should be present in the file “Pattern.txt” which is in the same directory with “IBPP.exe”. 

The “IBPP-SVM” uses several “images” to score the same sequence. Using a vector constituted by these scores, it applies support vector machine to predict promoter. To train the SVM model, training sets should be present in the directory “TrainSet”. In the “Pattern.txt” file, one or several “images” should be present, which determines the dimension of vectors.


Sheng Wang
Zhejiang University
College of Life Sciences
