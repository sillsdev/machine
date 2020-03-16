'''
Author: Andrew Thomas
Last Updated: March 2020
Instructions: run with `python(3) data_scraper.py [parent folder of results]`
'''

import sys
import os
import string
from pathlib import Path
from datetime import datetime

def scrapeData(path):
    cases = []
    # For each verse count of testing output
    for vCount in os.listdir(path):
        # Testing script procudes files for a "{0}-{1}" vCount which is ignored
        if vCount.find('{') == -1:
            for confidenceTuningFolder in os.listdir( 
                dataPath + vCount + '/test' ):

                data = {}
                data['VerseCount'] = vCount
                data['Confidence'] = confidenceTuningFolder[0:confidenceTuningFolder.find('-')]
                data['IsTuned'] = confidenceTuningFolder[confidenceTuningFolder.find('-') + 1:]

                with open( dataPath + vCount + '/test/' 
                    + confidenceTuningFolder + '/stdout' ) as resultFile:
                    for line in resultFile:
                        # If the line is something we care about
                        if line[0] != '/' and line.find('CSC') == -1:
                            # Some items start with dashes which we want to not scrape
                            startIdx = 1 if line[0] == '-' else 0
                            # Scrape the key and value and put it into data, removing all whitespace
                            data[line[startIdx:line.find( ':' )].translate( str.maketrans('', '', string.whitespace ))] = \
                                line[line.find(':') + 1:].strip()

                cases.append( data.copy() )
    return cases

def getVarNames(cases):
    variables = set( [] )
    # Grab all the variables collected from the cases
    for case in cases:
        [ variables.add( varName ) for varName in case.keys() ]

    # Remove header labels
    varFilter = ['CorrectSuggestionTypes', 'CorrectSuggestionN', '1']
    for i in varFilter:
        if i in variables:
            variables.remove(i)

    return variables

def writeAllData(variables, cases, languagePair):
    '''
    Writes all data to collected to a csv such that each output file from testing is one row

    Return: nothing
    '''
    with open( './scraper_output/' + getOutputFileName( languagePair, False ), 'w' ) as dataFile:
        for var in variables:
            dataFile.write(var + ',')

        for case in cases:
            dataFile.write('\n')
            for var in variables:
                if var in case.keys():
                    dataFile.write( case[var] )
                dataFile.write(',')

def writeFormattedForSpreadsheet(variables, cases, languagePair):
    '''
    Writes data formatted such that each row is a verse count and each column is a KSMR
        given a confidence-tuning combination
    
    Return: returns the number of confidence-tuning combinations without a known KSMR
    '''
    failCount = 0

    # Reorganize data
    limitedData = {}
    variables = set( [] )
    for case in cases:
        seriesName = case['Confidence'] + '-' + case['IsTuned']

        variables.add( seriesName )
        if not case['VerseCount'] in limitedData.keys():
            limitedData[case['VerseCount']] = {}

        limitedData[case['VerseCount']][seriesName] = case['KSMR']
        
    # Print to iile
    with open( './scraper_output/' + getOutputFileName( languagePair, True ), 'w' ) as formatFile:
        formatFile.write( 'VerseCount' )
        for var in variables:
            formatFile.write( ',' + var )

        for vCount in sorted( limitedData.keys() ):
            formatFile.write( '\n' + vCount)
            for var in variables:
                # If KSMR entry exists and is not blank
                if var in limitedData[vCount].keys() and limitedData[vCount][var]:
                    formatFile.write( ',' + limitedData[vCount][var] )
                else:
                    formatFile.write( ',' + 'n/a' )
                    failCount += 1
    return failCount

def getOutputFileName( lPair, isFormatted, delimiter = '_', ext = '.csv',
        outputFolder = './scraper_output' ):
    '''
    Builds the output file path

    Return: file path to output file
    '''

    Path(outputFolder).mkdir(parents=True, exist_ok=True)
    date = datetime.now().strftime( "%Y-%m-%d" )

    path = outputFolder
    path += languagePair
    path += delimiter
    if isFormatted:
        path += 'formatted' + delimiter
    path += date
    path += ext
    
    return path

if __name__ == 'main':
    dataPath = sys.argv[1]
    pathSplit = dataPath.split( os.path.sep )
    languagePair = pathSplit[-2] if dataPath[-1] == os.path.sep else pathSplit[-1]

    cases = scrapeData( dataPath )
    variables = getVarNames( cases )
    writeAllData( variables, cases, languagePair )
    failCount = writeFormattedForSpreadsheet( variables, cases, languagePair )

    if failCount:
        print(f'Failed to find KSMR data for {failCount} cases')