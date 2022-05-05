import os
import sys
import platform
import subprocess
import shutil
import codecs
import argparse

class ResourceCompiler (object):
	def __init__ (self, devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath):
		self.devKitPath = devKitPath
		self.languageCode = languageCode
		self.sourcesPath = sourcesPath
		self.resourcesPath = resourcesPath
		self.resourceObjectsPath = resourceObjectsPath
		self.resConvPath = None
		
	def IsValid (self):
		if self.resConvPath == None:
			return False
		if not os.path.exists (self.resConvPath):
			return False
		return True

	def GetPrecompiledResourceFilePath (self, grcFilePath):
		grcFileName = os.path.split (grcFilePath)[1]
		return os.path.join (self.resourceObjectsPath, grcFileName + '.i')

	def CompileLocalizedResources (self):
		locResourcesFolder = os.path.join (self.resourcesPath, 'R' + self.languageCode)
		grcFiles = self.CollectFilesFromFolderWithExtension (locResourcesFolder, '.grc')
		for grcFilePath in grcFiles:
			assert self.CompileResourceFile (grcFilePath), 'Failed to compile resource: ' + grcFilePath

	def CompileFixResources (self):
		fixResourcesFolder = os.path.join (self.resourcesPath, 'RFIX')
		grcFiles = self.CollectFilesFromFolderWithExtension (fixResourcesFolder, '.grc')
		for grcFilePath in grcFiles:
			assert self.CompileResourceFile (grcFilePath), 'Failed to compile resource: ' + grcFilePath

	def RunResConv (self, platformSign, codepage, inputFilePath, nativeResourceFileExtenion):
		imageResourcesFolder = os.path.join (self.resourcesPath, 'RFIX', 'Images')
		inputFileBaseName = os.path.splitext (os.path.split (inputFilePath)[1])[0]
		nativeResourceFilePath = os.path.join (self.resourceObjectsPath, inputFileBaseName + nativeResourceFileExtenion)
		print(f"{self.resConvPath} -m=r -T={platformSign} -q=utf8 {codepage} -w=2 -p={imageResourcesFolder} -i={inputFilePath} -o={nativeResourceFilePath}")
		result = subprocess.call ([
			self.resConvPath,
			'-m', 'r',						# resource compile mode
			'-T', platformSign,				# target platform
			'-q', 'utf8', codepage,			# code page conversion
			'-w', '2',						# HiDPI image size list
			'-p', imageResourcesFolder,		# image search path
			'-i', inputFilePath,			# input path
			'-o', nativeResourceFilePath	# output path
		])
		if result != 0:
			return False
		return True

	def CollectFilesFromFolderWithExtension (self, folderPath, extension):
		result = []
		for fileName in os.listdir (folderPath):
			fileExtension = os.path.splitext (fileName)[1]
			if fileExtension.lower () == extension.lower ():
				fullPath = os.path.join (folderPath, fileName)
				result.append (fullPath)
		return result

class WinResourceCompiler (ResourceCompiler):
	def __init__ (self, devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath):
		super (WinResourceCompiler, self).__init__ (devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath)
		self.resConvPath = os.path.join (devKitPath, 'Support', 'Tools', 'Win', 'ResConv.exe')

	def PrecompileResourceFile (self, grcFilePath):
		precompiledGrcFilePath = self.GetPrecompiledResourceFilePath (grcFilePath)
		result = subprocess.call ([
			'cl',
			'/nologo',
			'/X',
			'/EP',
			'/P',
			'/I', os.path.join (self.devKitPath, 'Support', 'Inc'),
			'/I', os.path.join (self.devKitPath, 'Support', 'Modules', 'DGLib'),
			'/I', self.sourcesPath,
			'/I', self.resourceObjectsPath,
			'/DWINDOWS',
			'/execution-charset:utf-8',
			'/Fi{}'.format (precompiledGrcFilePath),
			grcFilePath,
		])
		assert result == 0, 'Failed to precompile resource ' + grcFilePath
		return precompiledGrcFilePath

	def CompileResourceFile (self, grcFilePath):
		precompiledGrcFilePath = self.PrecompileResourceFile (grcFilePath)
		return self.RunResConv ('W', '1252', precompiledGrcFilePath, '.rc2')

	def GetNativeResourceFile (self):
		defaultNativeResourceFile = os.path.join (self.resourcesPath, 'RFIX.win', 'AddOnMain.rc2')
		if os.path.exists (defaultNativeResourceFile):
			return defaultNativeResourceFile

		existingNativeResourceFiles = self.CollectFilesFromFolderWithExtension (os.path.join (self.resourcesPath, 'RFIX.win'), '.rc2')
		assert existingNativeResourceFiles, 'Native resource file was not found at RFIX.win folder'

		return existingNativeResourceFiles[0]

	def CompileNativeResource (self, resultResourcePath):
		nativeResourceFile = self.GetNativeResourceFile ()
		result = subprocess.call ([
			'rc',
			'/i', os.path.join (self.devKitPath, 'Support', 'Inc'),
			'/i', os.path.join (self.devKitPath, 'Support', 'Modules', 'DGLib'),
			'/i', self.sourcesPath,
			'/i', self.resourceObjectsPath,
			'/fo', resultResourcePath,
			nativeResourceFile
		])
		assert result == 0, 'Failed to compile native resource ' + nativeResourceFile

class MacResourceCompiler (ResourceCompiler):
	def __init__ (self, devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath):
		super (MacResourceCompiler, self).__init__ (devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath)
		self.resConvPath = os.path.join (devKitPath, 'Support', 'Tools', 'OSX', 'ResConv')

	def PrecompileResourceFile (self, grcFilePath):
		precompiledGrcFilePath = self.GetPrecompiledResourceFilePath (grcFilePath)
		result = subprocess.call ([
			'clang',
			'-x', 'c++',
			'-E',
			'-P',
			'-Dmacintosh',
			'-I', os.path.join (self.devKitPath, 'Support', 'Inc'),
			'-I', os.path.join (self.devKitPath, 'Support', 'Modules', 'DGLib'),
			'-I', self.sourcesPath,
			'-I', self.resourceObjectsPath,
			'-o', precompiledGrcFilePath,
			grcFilePath,
		])
		assert result == 0, 'Failed to precompile resource ' + grcFilePath
		return precompiledGrcFilePath

	def CompileResourceFile (self, grcFilePath):
		precompiledGrcFilePath = self.PrecompileResourceFile (grcFilePath)
		return self.RunResConv ('M', 'utf16', precompiledGrcFilePath, '.ro')

	def CompileNativeResource (self, resultResourcePath):
		resultLocalizedResourcePath = os.path.join (resultResourcePath, 'English.lproj')
		if not os.path.exists (resultLocalizedResourcePath):
			os.makedirs (resultLocalizedResourcePath)
		resultLocalizableStringsPath = os.path.join (resultLocalizedResourcePath, 'Localizable.strings')
		resultLocalizableStringsFile = codecs.open (resultLocalizableStringsPath, 'w', 'utf-16')
		for fileName in os.listdir (self.resourceObjectsPath):
			filePath = os.path.join (self.resourceObjectsPath, fileName)
			extension = os.path.splitext (fileName)[1].lower ()
			if extension == '.tif':
				shutil.copy (filePath, resultResourcePath)
			elif extension == '.rsrd':
				shutil.copy (filePath, resultLocalizedResourcePath)
			elif extension == '.strings':
				stringsFile = codecs.open (filePath, 'r', 'utf-16')
				resultLocalizableStringsFile.write (stringsFile.read ())
				stringsFile.close ()
		resultLocalizableStringsFile.close ()

def Main (argv):
	parser = argparse.ArgumentParser (description = 'Archicad Add-On Resource Compiler.')
	parser.add_argument ('languageCode', help = 'Language code of the Add-On.')
	parser.add_argument ('devKitPath', help = 'Path of the Archicad Development Kit.')
	parser.add_argument ('sourcesPath', help = 'Path of the sources folder of the Add-On.')
	parser.add_argument ('resourcesPath', help = 'Path of the resources folder of the Add-On.')
	parser.add_argument ('resourceObjectsPath', help = 'Path of the folder to build resource objects.')
	parser.add_argument ('resultResourcePath', help = 'Path of the resulting resource.')
	args = parser.parse_args ()

	currentDir = os.path.dirname (os.path.abspath (__file__))
	os.chdir (currentDir)

	languageCode = args.languageCode
	devKitPath = os.path.abspath (args.devKitPath)
	sourcesPath = os.path.abspath (args.sourcesPath)
	resourcesPath = os.path.abspath (args.resourcesPath)
	resourceObjectsPath = os.path.abspath (args.resourceObjectsPath)
	resultResourcePath = os.path.abspath (args.resultResourcePath)

	resourceCompiler = None
	system = platform.system ()
	if system == 'Windows':
		resourceCompiler = WinResourceCompiler (devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath)
	elif system == 'Darwin':
		resourceCompiler = MacResourceCompiler (devKitPath, languageCode, sourcesPath, resourcesPath, resourceObjectsPath)

	assert resourceCompiler, 'Platform is not supported'
	assert resourceCompiler.IsValid (), 'Invalid resource compiler'

	resourceCompiler.CompileLocalizedResources ()
	resourceCompiler.CompileFixResources ()
	resourceCompiler.CompileNativeResource (resultResourcePath)

	return 0

sys.exit (Main (sys.argv))
