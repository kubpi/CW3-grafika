   M S B A M L   `   `   `  ����F  BCW3_grafika, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)clr-namespace:CW3_grafikaCW3_grafika  5   	       CW3_grafika.MainWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   +localclr-namespace:CW3_grafika     5   	     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)    � DataContext 5	          CW3_grafika.ImageViewModel  5
   
   .��5      �  � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��auto �6   �� 5      $��* �6   .+�5   
   ��  �� Orientation$ 
Horizontal=�6   $��0��6.   .+�5      �� $��
Load Image��6   $
��Left=�6O   $��0,10,0,0q�6j   $	��Top=�6|   ��6*   �� *LoadImageCommand+6|   �� 5      $��
Save Image��6   $
��Left=�6O   $��	10,10,0,0q�6j   $	��Top=�6}   ��6*   �� *SaveImageCommand+6}   5      �� 5   
   $��Stretch=�6   $��Stretch=�6.   $��1��6J   $��1��6W   $��Uniform=�6�    �� BitmapScalingMode$ NearestNeighbor=�6�   ��6g   �� *ConvertedPbmImage+6�   5      5      