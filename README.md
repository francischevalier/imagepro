ImagePro
========

Un peu comme le visualisateur d’images de Windows, ce gestionnaire permet d’afficher, à l’aide de petites vignettes, les dossiers ainsi que les images qu’ils contiennent. En cliquant sur une image, on obtient des informations sur celle-ci. On peut également visualiser les images en plein-écran en mode carousel.

Ce programme cherche les dossiers ainsi que les images qui s’y trouvent. Il dessine une vignette pour chaque dossier et image. En prenant compte du nombre d’éléments à afficher et la résolution de l’écran, il détermine le nombre de vignettes par ligne ainsi que le nombre de pages. À des fins d’optimisation, le programme enregistre une capture d’écran pour chacune des pages dans le but de redessiner les pages plus rapidement par la suite. Il est également possible d’imprimer un rapport qui correspond à une liste d’images avec leurs informations respectives (taille, dimensions et nom).

Aucun dossier d’images n’a été inclus afin de ne pas inutilement augmenter la taille du fichier zip. Par contre, pour tester rapidement le programme, il suffit de mettre un ou plusieurs dossiers contenant des images dans le dossier «Debug» (chemin relatif ImagePro\ImagePro\bin\Debug) et de lancer le fichier exécutable qui s’y trouve.

Auteurs : David Lefaivre-B. et Francis Chevalier<br />
Création : 17 novembre 2010<br />
Dernière modification : 21 novembre 2010<br />
Développement : Visual Studio 2010 / C#
