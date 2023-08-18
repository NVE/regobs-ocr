import * as fs from 'fs';
import * as path from 'path';
import { SnowProfile } from './snow_profile_t';

function parse(filename: string): SnowProfile {
    /**********************/
    /* Magic happens here */
    /**********************/

    // And then some demo code. Remove before flight.
    const dirname: string = path.dirname(filename);
    const basename: string = path.basename(filename);
    const imgId: string = basename.replace(/\D+/g, '');
    const correctDataFilename: string = path.join(dirname, `${imgId}.json`);
    const correctFile: Buffer = fs.readFileSync(correctDataFilename);
    const correctData: SnowProfile = JSON.parse(correctFile.toString());
    return correctData;
}


const filenameToParse: string = process.argv[2];
const parsedData: SnowProfile = parse(filenameToParse);
console.log(parsedData);
