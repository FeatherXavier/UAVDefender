from genericpath import isfile
import json
import shutil
import os

def coordinates2yolo(xmin,ymin,xmax,ymax,img_w,img_h,dataFile):
    # 保留6位小数
    x = round((xmin+xmax)/(2.0*img_w),6)
    y = round((ymin+ymax)/(2.0*img_h),6)
    w1 = round((xmax-xmin)/(1.0*img_w),6)
    h1 = round((ymax-ymin)/(1.0*img_h),6)
    print(0,x,y,w1,h1, file=dataFile)
    return x,y,w1,h1

ROOT_ROOT_DIR = 'E:\\UAVDataset\\validation\\'

for i in range(len(os.listdir(ROOT_ROOT_DIR)) - 1):
    ROOT_DIR = ROOT_ROOT_DIR + os.listdir(ROOT_ROOT_DIR)[i] + "\\"
    
    if not os.path.exists(ROOT_DIR + 'labels\\') and not os.path.isfile(ROOT_DIR):
        os.mkdir(ROOT_DIR + 'labels\\')
        
    if not os.path.exists(ROOT_DIR + 'images/'):
        os.mkdir(ROOT_DIR + 'images\\')
    
  
    with open(ROOT_DIR + "IR_label.json") as f:
        jsonraw = json.loads(f.read())

    filenames = os.listdir(ROOT_DIR)

    print(len(filenames))

    print(len(jsonraw['gt_rect']))
    try:
        for n in range(len(filenames)):
            if filenames[n] == "info.dat" or filenames[n] == "lables" or filenames[n] == "IR_lable.json":
                continue
            
            #shutil.copyfile(ROOT_DIR + str(filenames[n]), ROOT_DIR + "images/" + filenames[n])
            os.system("copy " + (ROOT_DIR + filenames[n]) + " " +  ROOT_DIR + "images\\")
            
            if(jsonraw['exist'][n]) == 0:
                print(ROOT_DIR + filenames[n] + " EMPTY")
                continue

            with open(ROOT_DIR + "labels\\" + str((filenames[n].split('.')[0]))+ ".txt", 'w') as lableData:
                gt_rect = jsonraw['gt_rect'][n]
                x,y,w,h = coordinates2yolo(gt_rect[0],gt_rect[1],gt_rect[2] + gt_rect[0],gt_rect[3] + gt_rect[1],640,512,lableData)

    except:
        print("ERROR")
        
