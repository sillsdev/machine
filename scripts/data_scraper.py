import sys
import os
from datetime import datetime

dataPath = sys.argv[1]

cases = []

for vCount in os.listdir(dataPath):
    
    for confidenceTuningFolder in os.listdir( 
        dataPath + vCount + '/test' ):

        data = {}
        data['VerseCount'] = vCount
        data['Confidence'] = confidenceTuningFolder[0:confidenceTuningFolder.find('-')]
        data['IsTuned'] = confidenceTuningFolder[confidenceTuningFolder.find('-') + 1:]

        with open( dataPath + vCount + '/test/' 
            + confidenceTuningFolder + '/stdout' ) as resultFile:
            for line in resultFile:
                if line[0] != '/' and line.find('CSC') == -1:
                    data[line[0:line.find(':')]] = line[line.find(':') + 1:].strip()

        cases.append( data.copy() )

variables = set( [] )
for case in cases:
    [ variables.add( varName ) for varName in case.keys() ]

varFilter = ['Correct Suggestion Types', 'Correct Suggestion N', '-1']
for i in varFilter:
    variables.remove(i)

fileName = datetime.now().strftime("%Y-%m-%d%H:%M:%S") + '.csv'

with open( fileName, 'w' ) as dataFile:
    for var in variables:
        dataFile.write(var + ',')

    for case in cases:
        dataFile.write('\n')
        for var in variables:
            if var in case.keys():
                dataFile.write( case[var] )
            dataFile.write(',')

limitedData = {}
variables = set( [] )
for case in cases:
    if not case['VerseCount'] in limitedData.keys():
        limitedData[case['VerseCount']] = {}
    ksmr = case['KSMR'] if 'KSMR' in case.keys() else 'n/a'
    limitedData[case['VerseCount']][case['Confidence'] + '-' + case['IsTuned']] = ksmr
    variables.add( case['Confidence'] + '-' + case['IsTuned'] )

with open( 'formatted-' + fileName, 'w' ) as formatFile:
    formatFile.write('KSMR')
    for var in variables:
        formatFile.write( ',' + var )

    for vCount in sorted( limitedData.keys() ):
        formatFile.write( '\n' + vCount)

        for var in variables:
            formatFile.write( ',' + limitedData[vCount][var] )