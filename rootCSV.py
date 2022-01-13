import os
import csv
import argparse
from datetime import date
import copy

filesProcessed = 0
folder = []
fls_intersected = []
batch = list()

# find files and folders
def findFolders():
    for i in os.walk(os.getcwd()):
        folder.append(i)

# create batch
def fillBatch( results, filesProcessed ):
    for address, dirs, files in folder:
        for file in files:
    #        print(file)
            ext = os.path.splitext(address+'/'+file)[1]
            if(ext == '.csv'):
     #           print(ext)
     #           print(file.find(results.simple_value) >= 0)

                    with open(address+'/'+file, newline='') as csvfile:
                        if (results.simple_value == 'All' or file.find(results.simple_value) >= 0):
                            sskareader = csv.reader(csvfile, delimiter=';', quotechar='"')
                            rowsInCSV = 0
                            for row in sskareader:
                              #  print('| '.join(row))
                                konto = row[0]
                                splittedDate = (row[2]).split(".")
                                if (sskareader.line_num > 1 and len(splittedDate) != 3):
                                    print('!',filesProcessed, '! Row:', sskareader.line_num, '!! ValueDate:', splittedDate, '!!', file, '! ReservationDate:', row[1])
                                    rowsInCSV -=1
                                if (sskareader.line_num > 1 and len(splittedDate) == 3):
                                    rowsInCSV += 1
                                    currDate = formatDate(splittedDate) 
                                 # print(sskareader.line_num, currDate)
                                    if (sskareader.line_num == 2):
                                        mindate = currDate
                                        maxdate = mindate
                                    elif (currDate > maxdate):
                                        maxdate = currDate
                                    elif (currDate < mindate):
                                        mindate = currDate
                                else:
                                    fieldsQty = len(row)
                            filesProcessed += 1
                            fls = ( filesProcessed, mindate, fieldsQty, maxdate, file, fls_intersected, konto, rowsInCSV  )
                            batch.append(copy.deepcopy(fls))
                            print(99*'-')
                            print("|{:^5}| {:%Y-%m-%d} |{:^8}| {:%Y-%m-%d} | {:^6} | {:55} " .format(filesProcessed, mindate, fieldsQty, maxdate, rowsInCSV, file ))
                    csvfile.close()
    
# form batch
def processBatch(warnOverlaps):
    batch2 = copy.deepcopy(batch)
    for line in batch:
        lineKonto = line[6][-9:]
        # print("batch line:", line[0])
        for nr in batch2:
            if(line[0] != nr[0]):
                # print("batch2 nr", nr[0])
                nrKonto = nr[6][-9:]    
                if( line[3] > nr[1] and line[1] < nr[3] and line[1] < nr[1] and lineKonto == nrKonto and line[2] != nr[2]):
                    print("\n", line[0], line[1], line[3], lineKonto, line[2], "rows:", line[7], "|", line[5], "|", nr[0], nr[1], nr[3], nrKonto, nr[2], "rows:", nr[7], nr[5] )
                    # print("external:", line[0], "inner:", nr[0], "overlapped:", line[5], "|", nr[5])
                    if(len(line[5]) == 0):
                        line[5].append(nr[4])
                        print("overlapping first entry row:", line[0], "|", line[5] )
                    else:
                        line[5].append(nr[4])
                        print("overlapping is appended in row:", line[0], "|", line[5] )
                    warnOverlaps += 1
        # print("batch line 5:", line[5])
    # print("warnOverlaps:", warnOverlaps)
    return warnOverlaps
# filter batch
def filterProcessedBatch(warnOverlaps):
    while ((len(batch) != warnOverlaps) or (len(batch) == 0)):
        for ln in batch:
            # print( "viewed row: ", ln[0], "|", ln[5])
            if( len(ln[5]) == 0 ):
                # print( "removed row: ", ln[0], "index:", batch.index(ln))
                batch.remove(ln)
                # print("batch after remove, len:", len(batch))
                break

# handle batch insert empty string in ValueDate field for overlappings
def useBatch(currDate):
    for ln in batch:
        print("|{:^5}| {:%Y-%m-%d} |{:^8}| {:%Y-%m-%d} | {:^6} | {} | {:55} " .format(ln[0], ln[1], ln[2], ln[3], ln[7], ln[5], ln[4] ))
        
        thresholdDate = copy.deepcopy(ln[3])                    
        for fl in ln[5]:
            nextFile = copy.deepcopy(fl)
            print(nextFile)
            print("ThresholdDate:", thresholdDate)
            print(100*'-')
            for address, dirs, files in folder:
                if( os.path.exists(address+'\\'+nextFile) ):
                    with open(address+'\\'+nextFile, newline='') as csvfile:
                        sskareader = csv.reader(csvfile, delimiter=';', quotechar='"')
                        for row in sskareader:
                            splittedDate = (row[2]).split(".") # Y, M, D
                            if (sskareader.line_num > 1 and len(splittedDate) != 3):
                                print('!',filesProcessed, '! Row:', sskareader.line_num, '!! ValueDate:', splittedDate, '!!', nextFile, '! ReservationDate:', row[1])
                            if (sskareader.line_num > 1 and len(splittedDate) == 3):
                                currDate = formatDate(splittedDate)                                
                                # print("ThresholdDate:", thresholdDate, "Current Date:", sskareader2.line_num, "|", currDate)

                            if((currDate <= thresholdDate) and (sskareader.line_num > 1)):
                                # row[2] = copy.deepcopy(\"\")
                                print("empty Value date inserted:", row[2],  "Reservation date:", row[1])
                                    
                    csvfile.close() 
                    print("\n Next File:")
                    print(100*'+')

def formatDate(splittedDate):
    year = int('20' + splittedDate[2]) if (len(splittedDate[2]) == 2) else int(splittedDate[2])
    month = int(splittedDate[1])
    day = int(splittedDate[0])
    if (month == 2 and day == 29 and (year%4 != 0 or year == 2000)):
        day = 1
        month = 3
    # print("Formatted Current Date:",  date(year, month, day))
    return date(year, month, day)
     
    
def main():

    currDate = date.min
    mindate = date.min
    maxdate = date.max
    fieldsQty = int()
    konto = []
    fls = list()
    warnOverlaps = 0
    rowsInCSV = 0

    parser = argparse.ArgumentParser()

    parser.add_argument('-s', action='store', dest='simple_value',
                    default='All',
                    help='Process only files contained input'
                    )
# -s             : name of optional(short -s or long version --save) argumrnt
# action         : default=store -> save in namespace
# type=int       : optional type (e.g. int) of arg
# dest           : The name of the attribute to be added to the object returned by parse_args()
# default='All'  : The value (e.g. All) produced if the argument is absent from the command line

    results = parser.parse_args()
# print('simple_value     =', results.simple_value)
# print('collection       =', results.collection)

    print("Current working Directory : %s" % os.getcwd())
    print(100*'+')
    print('| Nr. | Begin date | ColQty |  End date  |  Rows  | File Name  ')

    findFolders()
    fillBatch(results, filesProcessed )

    print(100*'*')
    print('Processed: ', filesProcessed, 'files')
    print(100*'*')
    
    warnOverlaps = processBatch(warnOverlaps)
#    print("warnOverlaps:", warnOverlaps)

    filterProcessedBatch(warnOverlaps)

    print(100*'*')
    print("Overlapped Date intervals for different file types:", warnOverlaps)
    print("length of filtered batch:", len(batch))
    print(100*'|')
    useBatch(currDate)

# entry point main
main()