#import <AVFoundation/AVFoundation.h>
#import <Foundation/Foundation.h>
#include <stdlib.h>
#include <string.h>

extern "C" const char* WwiseUnity_GetAudioSessionInfo()
{
	@autoreleasepool
	{
		AVAudioSession *session = [AVAudioSession sharedInstance];
		NSString *category = session.category ?: @"";
		NSString *mode = session.mode ?: @"";
		AVAudioSessionCategoryOptions options = session.categoryOptions;
		AVAudioSessionRouteDescription *route = session.currentRoute;

		NSMutableArray<NSString *> *outputs = [NSMutableArray array];
		for (AVAudioSessionPortDescription *output in route.outputs)
		{
			NSString *entry = [NSString stringWithFormat:@"%@(%@)", output.portType, output.portName];
			[outputs addObject:entry];
		}

		NSMutableArray<NSString *> *inputs = [NSMutableArray array];
		for (AVAudioSessionPortDescription *input in route.inputs)
		{
			NSString *entry = [NSString stringWithFormat:@"%@(%@)", input.portType, input.portName];
			[inputs addObject:entry];
		}

		NSString *outputDesc = outputs.count > 0 ? [outputs componentsJoinedByString:@", "] : @"none";
		NSString *inputDesc = inputs.count > 0 ? [inputs componentsJoinedByString:@", "] : @"none";
		NSString *desc =
			[NSString stringWithFormat:@"AVAudioSession category=%@ mode=%@ options=0x%lx outputs=[%@] inputs=[%@]",
			 category, mode, (unsigned long)options, outputDesc, inputDesc];

		const char *utf8 = [desc UTF8String];
		if (!utf8)
		{
			return nullptr;
		}

		char *copy = (char *)malloc(strlen(utf8) + 1);
		if (!copy)
		{
			return nullptr;
		}

		strcpy(copy, utf8);
		return copy;
	}
}

extern "C" void WwiseUnity_FreeCString(const char *str)
{
	if (str)
	{
		free((void *)str);
	}
}
