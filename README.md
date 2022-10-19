# DistributedSearch

The algorithm we used in order to find the index of the first appearance of the requested sequence is the following:</br></br>
We use the already existing Thread pool that creates and manages the number of threads we input as the parameters.</br>
Each thread read a block of data from the stream reader into a personal 10k buffer in its turn (using the waitHandle), to avoid conflicting reading and then releases it while it moves on to search the requested character sequence in its buffer.
If the sequence is found it calculates the index where the sequence started by using its inner index count and the block number it was assigned, storing the complete index in a list (using the mutex to avoid conflicting adding to the list).</br></br>
This is done because it is possible multiple threads will find the same word in different blocks, and the word might appear in the latter block earlier than it is in the former, despite the former being the true first occurrence (as in, a word being found first in the run time by a thread does not necessarily means it is first in appearance order).</br>
At the end after finding the word (or reaching the end of the file) we will sort the list that holds the found indexes and return the first,the lowest index, hence the true earliest occurrence of the requested sequence.

</br></br>


<p align="center">
<img width="659" alt="image" src="https://user-images.githubusercontent.com/81624047/196717609-d3ad3c5a-7ab5-4703-b719-ca4b608ab5c3.png">
</p>
